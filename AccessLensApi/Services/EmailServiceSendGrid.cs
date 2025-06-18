using AccessLensApi.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace AccessLensApi.Services
{
    public sealed class SendGridEmailService : IEmailService
    {
        private readonly ISendGridClient _client;
        private readonly string _from;
        private readonly IConfiguration _config;

        public SendGridEmailService(IConfiguration cfg)
        {
            _config = cfg ?? throw new ArgumentNullException(nameof(cfg));
            var apiKey = cfg["SendGrid:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid ApiKey configuration is missing");
            _client = new SendGridClient(apiKey);
            _from = cfg["SendGrid:FromEmail"] ?? throw new InvalidOperationException("SendGrid FromEmail configuration is missing");
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_from),
                Subject = subject,
                HtmlContent = body
            };
            msg.AddTo(new EmailAddress(to));
            var response = await _client.SendEmailAsync(msg);
            if ((int)response.StatusCode >= 400)
                throw new InvalidOperationException($"SendGrid failed: {(int)response.StatusCode}");
        }

        public async Task SendMagicLinkAsync(string email, string magicToken)
        {
            var baseUrl = _config["Urls:BaseUrl"];
            var magicLink = $"{baseUrl.TrimEnd('/')}/auth/magic/{magicToken}";

            var subject = "Your Access Lens Magic Link";
            var htmlBody = $@"
                <html>
                  <body>
                    <h2>Welcome to Access Lens!</h2>
                    <p>Click the link below to sign in to your account:</p>
                    <p>
                      <a href=""{WebUtility.HtmlEncode(magicLink)}""
                         style=""display:inline-block;
                                 padding:12px 24px;
                                 background:#0078d7;
                                 color:white;
                                 text-decoration:none;
                                 border-radius:6px;
                                 font-weight:bold;"">
                        Sign In to Access Lens
                      </a>
                    </p>
                    <p>This link will expire in 15 minutes for security.</p>
                    <p>If you didn't request this link, you can safely ignore this email.</p>
                    <hr>
                    <p style=""color:#666;font-size:12px;"">
                      If the button doesn't work, copy and paste this link into your browser:<br>
                      {WebUtility.HtmlEncode(magicLink)}
                    </p>
                  </body>
                </html>";

            await this.SendAsync(email, subject, htmlBody);
        }

        public async Task SendScanResultEmailAsync(string email, string presignedPdfUrl, int score, string presignedTeaserUrl = null)
        {
            var subject = "Your Access Lens WCAG Snapshot is Ready";
            var teaserImgHtml = presignedTeaserUrl != null
                ? $"<img src=\"{WebUtility.HtmlEncode(presignedTeaserUrl)}\" alt=\"Teaser\" style=\"max-width:600px;\"/><br/>"
                : string.Empty;

            var htmlBody = $@"
                <html>
                  <body>
                    <p>Your WCAG Snapshot score is <strong>{score}/100</strong>.</p>
                    {teaserImgHtml}
                    <p><a href=""{WebUtility.HtmlEncode(presignedPdfUrl)}"" style=""display:inline-block;padding:12px 20px;background:#0078d7;color:white;text-decoration:none;border-radius:4px;"">
                      Download PDF Report
                    </a></p>
                    <p>Thanks for using Access Lens!</p>
                  </body>
                </html>";

            await this.SendAsync(email, subject, htmlBody);
        }
    }
}
