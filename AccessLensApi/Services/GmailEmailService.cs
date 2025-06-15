using AccessLensApi.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AccessLensApi.Services
{
    public class GmailEmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly IConfiguration _config;

        // Gmail API scope for sending mail
        private readonly string GmailSendScope = GmailService.Scope.GmailSend;

        public GmailEmailService(IConfiguration configuration)
        {
            _config = configuration;
            // "FromEmail" is the single address you send from
            _fromEmail = configuration["Gmail:FromEmail"]
                ?? Environment.GetEnvironmentVariable("GMAIL_FROM_EMAIL")
                ?? throw new ArgumentNullException("Gmail:FromEmail must be set via config or GMAIL_FROM_EMAIL");

            // Client ID/Secret from Google Cloud (OAuth)
            _clientId = configuration["Gmail:OAuthClientId"]
                ?? Environment.GetEnvironmentVariable("GMAIL_OAUTH_CLIENT_ID")
                ?? throw new ArgumentNullException("Gmail:OAuthClientId must be set via config or GMAIL_OAUTH_CLIENT_ID");
            _clientSecret = configuration["Gmail:OAuthClientSecret"]
                ?? Environment.GetEnvironmentVariable("GMAIL_OAUTH_CLIENT_SECRET")
                ?? throw new ArgumentNullException("Gmail:OAuthClientSecret must be set via config or GMAIL_OAUTH_CLIENT_SECRET");

            // A long-lived refresh token you obtained once via the OAuth consent flow
            _refreshToken = configuration["Gmail:RefreshToken"]
                ?? Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN")
                ?? throw new ArgumentNullException("Gmail:RefreshToken must be set via config or GMAIL_REFRESH_TOKEN");
        }

        public Task SendAsync(string to, string subject, string body)
        {
            throw new NotImplementedException();
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

            await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task SendScanResultEmailAsync(string email, string presignedPdfUrl, int score, string presignedTeaserUrl = null)
        {
            var subject = "Your Access Lens WCAG Snapshot is Ready";

            var teaserHtml = "";
            if (!string.IsNullOrEmpty(presignedTeaserUrl))
            {
                teaserHtml = $@"
                    <img src=""{WebUtility.HtmlEncode(presignedTeaserUrl)}"" 
                         alt=""Teaser"" 
                         style=""max-width:600px;""/><br/>";
            }

            var htmlBody = $@"
                <html>
                  <body>
                    <p>Your WCAG Snapshot score is <strong>{score}/100</strong>.</p>
                    {teaserHtml}
                    <p>
                      <a href=""{WebUtility.HtmlEncode(presignedPdfUrl)}"" 
                         style=""display:inline-block;
                                 padding:12px 20px;
                                 background:#0078d7;
                                 color:white;
                                 text-decoration:none;
                                 border-radius:4px;"">
                        Download PDF Report
                      </a>
                    </p>
                    <p>Thanks for using Access Lens!</p>
                  </body>
                </html>";

            await SendEmailAsync(email, subject, htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // 1) Create a Google credential from your client ID/secret + saved refresh token
            var credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _clientId,
                            ClientSecret = _clientSecret
                        },
                        Scopes = new[] { GmailSendScope }
                    }),
                "user", // arbitrary user ID
                new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                {
                    RefreshToken = _refreshToken
                });

            // 2) Create Gmail service
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccessLens"
            });

            // 3) Build the email message
            var message = new Message();

            var email = new StringBuilder();
            email.AppendLine($"From: {_fromEmail}");
            email.AppendLine($"To: {toEmail}");
            email.AppendLine($"Subject: {subject}");
            email.AppendLine("Content-Type: text/html; charset=utf-8");
            email.AppendLine();
            email.AppendLine(htmlBody);

            // 4) Encode and send
            var encodedEmail = Convert.ToBase64String(Encoding.UTF8.GetBytes(email.ToString()))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            message.Raw = encodedEmail;

            await service.Users.Messages.Send(message, "me").ExecuteAsync();
        }
    }
}