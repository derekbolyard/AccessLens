using AccessLensApi.Data;
using AccessLensApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using Stripe.V2;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly string _webhookSecret;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IConfiguration config,
            ApplicationDbContext dbContext,
            ILogger<StripeWebhookController> logger)
        {
            _webhookSecret = config["Stripe:WebhookSecret"];
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // 1) Read the raw JSON from the request body
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // 2) Verify the Stripe signature
            Stripe.Event stripeEvent;
            try
            {
                var signature = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Stripe webhook signature.");
                return BadRequest();
            }

            // 3) Switch on the event type
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionAsync(stripeEvent.Data.Object as Session);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(json);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(json);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }

        private async Task HandleCheckoutSessionAsync(Session session)
        {
            if (session == null)
                return;

            var email = session.CustomerDetails?.Email;
            if (string.IsNullOrEmpty(email))
                return;

            // We assume the front-end set metadata["type"] = "snapshot" or "subscription"
            if (!session.Metadata.TryGetValue("type", out var type))
                return;

            if (type == "snapshot")
            {
                var snap = new SnapshotPass
                {
                    Email = email,
                    CreditsLeft = 1,
                    StripeCustomerId = session.CustomerId
                };
                _dbContext.SnapshotPasses.Add(snap);
            }
            else if (type == "subscription")
            {
                var existing = await _dbContext.Subscriptions
                                     .FirstOrDefaultAsync(s => s.Email == email);
                if (existing == null)
                {
                    existing = new Models.Subscription
                    {
                        Email = email,
                        Active = true,
                        StripeSubId = session.SubscriptionId,
                        NextBillingUtc = DateTime.UtcNow.AddMonths(1)
                    };
                    _dbContext.Subscriptions.Add(existing);
                }
                else
                {
                    existing.Active = true;
                    existing.StripeSubId = session.SubscriptionId;
                    existing.NextBillingUtc = DateTime.UtcNow.AddMonths(1);
                    _dbContext.Subscriptions.Update(existing);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleInvoicePaymentFailedAsync(string rawJson)
        {
            // Parse the raw JSON rather than relying on a typed property
            // so we can always grab the "subscription" field if it exists.
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(rawJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Invoice JSON.");
                return;
            }

            // Navigate to data.object.invoice (the top‐level "data" → "object" for this event)
            if (!doc.RootElement.TryGetProperty("data", out var dataElem) ||
                !dataElem.TryGetProperty("object", out var invoiceElem))
            {
                _logger.LogWarning("Webhook JSON missing data.object for invoice.payment_failed.");
                return;
            }

            // Attempt to extract the subscription ID from the invoice JSON
            string subId = null;
            if (invoiceElem.TryGetProperty("subscription", out var subProp) &&
                subProp.ValueKind == JsonValueKind.String)
            {
                subId = subProp.GetString();
            }

            if (string.IsNullOrEmpty(subId))
            {
                // This invoice was not tied to any subscription
                _logger.LogInformation("Invoice has no subscription field—skipping payment_failed handling.");
                return;
            }

            // Mark that subscription as inactive
            var existing = await _dbContext.Subscriptions
                                 .FirstOrDefaultAsync(s => s.StripeSubId == subId);
            if (existing != null)
            {
                existing.Active = false;
                _dbContext.Subscriptions.Update(existing);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task HandleSubscriptionUpdatedAsync(string rawJson)
        {
            // Parse raw JSON to extract the subscription object and its fields
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(rawJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Subscription JSON.");
                return;
            }

            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var dataElem) ||
                !dataElem.TryGetProperty("object", out var subElem))
            {
                _logger.LogWarning("Webhook JSON missing data.object for customer.subscription.updated.");
                return;
            }

            // Extract subscription ID
            if (!subElem.TryGetProperty("id", out var idProp) ||
                idProp.ValueKind != JsonValueKind.String)
            {
                _logger.LogWarning("customer.subscription.updated: missing id field");
                return;
            }
            var subId = idProp.GetString();

            // Look up our subscription row
            var existing = await _dbContext.Subscriptions
                                 .FirstOrDefaultAsync(s => s.StripeSubId == subId);
            if (existing == null)
                return;

            // Extract "status"
            string status = null;
            if (subElem.TryGetProperty("status", out var statusProp) &&
                statusProp.ValueKind == JsonValueKind.String)
            {
                status = statusProp.GetString();
            }

            // Extract "current_period_end" (Unix timestamp)
            long? epochEnd = null;
            if (subElem.TryGetProperty("current_period_end", out var endProp) &&
                (endProp.ValueKind == JsonValueKind.Number) &&
                endProp.TryGetInt64(out var val))
            {
                epochEnd = val;
            }

            // Update Active flag based on status
            if (status == "active" || status == "trialing")
            {
                existing.Active = true;

                if (epochEnd.HasValue)
                {
                    existing.NextBillingUtc = DateTimeOffset
                        .FromUnixTimeSeconds(epochEnd.Value)
                        .UtcDateTime;
                }
            }
            else
            {
                existing.Active = false;
            }

            _dbContext.Subscriptions.Update(existing);
            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription stripeSub)
        {
            if (stripeSub == null)
                return;

            var subId = stripeSub.Id;
            var existing = await _dbContext.Subscriptions
                                 .FirstOrDefaultAsync(s => s.StripeSubId == subId);
            if (existing != null)
            {
                existing.Active = false;
                _dbContext.Subscriptions.Update(existing);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
