using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using AccessLensApi.Services.Interfaces;

namespace AccessLensApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly string _fromAddress;

        public EmailService(IAmazonSimpleEmailService sesClient, IConfiguration config)
        {
            _sesClient = sesClient;
            _fromAddress = config["AWS:SesFromEmail"];
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var subject = "Your Access Lens Verification Code";
            var htmlBody = $@"
                <html>
                  <body>
                    <p>Your Access Lens 6-digit verification code is:</p>
                    <h2>{WebUtility.HtmlEncode(code)}</h2>
                    <p>This code expires in 15 minutes.</p>
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
