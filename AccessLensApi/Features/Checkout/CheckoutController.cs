using AccessLensApi.Config;
using AccessLensApi.Features.Checkout.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AccessLensApi.Features.Checkout
{

    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController(SessionService sessionService,
        IOptions<UrlOptions> urlOptions,
        IOptions<StripeOptions> stripeOptions) : ControllerBase
    {
        private readonly UrlOptions _urls = urlOptions.Value;
        private readonly StripeOptions _stripeOptions = stripeOptions.Value;

        [HttpPost("api/create-checkout-session")]
        public IActionResult CreateCheckoutSession([FromBody] CheckoutRequest req)
        {
            StripeConfiguration.ApiKey = _stripeOptions.ApiKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                LineItems =
                [
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = 4500, // $45.00
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Website Accessibility Scan"
                            }
                        },
                        Quantity = 1
                    }
                ],
                Mode = "payment",
                SuccessUrl = $"{_urls.WebAppUrl}/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_urls.MarketingUrl}/canceled",
                Metadata = new Dictionary<string, string>
                {
                    { "scanId", req.ScanId.ToString() },
                    { "email", req.Email }
                }
            };

            var session = sessionService.Create(options);
            return Ok(new { sessionId = session.Id });
        }
    }
}
