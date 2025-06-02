using AccessLensApi.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.IO;
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

        // Gmail API scope for sending mail
        private readonly string GmailSendScope = GmailService.Scope.GmailSend;

        public GmailEmailService(IConfiguration configuration)
        {
            // “FromEmail” is the single address you send from
            _fromEmail = configuration["Gmail:FromEmail"]
                ?? throw new ArgumentNullException("Gmail:FromEmail must be set");

            // Client ID/Secret from Google Cloud (OAuth)
            _clientId = configuration["Gmail:OAuthClientId"]
                ?? throw new ArgumentNullException("Gmail:OAuthClientId must be set");
            _clientSecret = configuration["Gmail:OAuthClientSecret"]
                ?? throw new ArgumentNullException("Gmail:OAuthClientSecret must be set");

            // A long-lived refresh token you obtained once via the OAuth consent flow
            _refreshToken = configuration["Gmail:RefreshToken"]
                ?? throw new ArgumentNullException("Gmail:RefreshToken must be set");
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
            var tokenResponse = new TokenResponse { RefreshToken = _refreshToken };
            var clientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { GmailSendScope }
                // We don't need a data store because we already have the refresh token
            });

            var credential = new UserCredential(flow, _fromEmail, tokenResponse);

            // 2) Ensure the access token is valid (refresh if needed)
            bool success = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!success)
                throw new InvalidOperationException("Unable to refresh Gmail access token.");

            // 3) Create the Gmail API service using the credential
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AccessLens"
            });

            // 4) Build the MIME message via MimeKit
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            // 5) Encode to base64url (RFC 4648 §5)
            byte[] rawBytes;
            using (var ms = new MemoryStream())
            {
                await message.WriteToAsync(ms);
                rawBytes = ms.ToArray();
            }
            var base64Url = Convert.ToBase64String(rawBytes)
                               .Replace('+', '-')
                               .Replace('/', '_')
                               .TrimEnd('=');

            // 6) Send the message via Gmail API
            var gmailMessage = new Message { Raw = base64Url };
            await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
        }
    }
}
