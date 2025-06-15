using AccessLensApi.Services.Interfaces;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Net;

namespace AccessLensApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly string _fromAddress;
        private readonly IConfiguration _config;

        public EmailService(IAmazonSimpleEmailService sesClient, IConfiguration config)
        {
            _sesClient = sesClient;
            _fromAddress = config["AWS:SesFromEmail"];
            _config = config;
        }

        public async Task SendMagicLinkAsync(string email, string magicToken)
        {
            var baseUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var magicLink = $"{baseUrl.TrimEnd('/')}/api/auth/magic/{magicToken}";

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

            var sendRequest = new SendEmailRequest
            {
                Source = _fromAddress,
                Destination = new Destination { ToAddresses = new List<string> { email } },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body { Html = new Content(htmlBody) }
                }
            };

            await _sesClient.SendEmailAsync(sendRequest);
        }

        public async Task SendScanResultEmailAsync(string email, string presignedPdfUrl, int score, string presignedTeaserUrl = null)
        {
            var subject = "Your Access Lens WCAG Snapshot is Ready";
            var teaserImgHtml = presignedTeaserUrl != null
                ? $"<img src=\"{WebUtility.HtmlEncode(presignedTeaserUrl)}\" alt=\"Teaser\" style=\"max-width:600px;\"/><br/>"
                : "";

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

            var sendRequest = new SendEmailRequest
            {
                Source = _fromAddress,
                Destination = new Destination { ToAddresses = new List<string> { email } },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body { Html = new Content(htmlBody) }
                }
            };

            await _sesClient.SendEmailAsync(sendRequest);
        }
    }
}