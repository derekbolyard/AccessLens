using AccessLensApi.Features.Scans.Models;
using Moq;
using Moq.Protected;
using System.Net;

namespace AccessLensApi.Tests.Helpers
{
    public static class ScanHelper
    {
        public static void SetupMockCaptcha(
            Mock<IHttpClientFactory> httpFactory,
            bool captchaSuccess)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                   .Setup<Task<HttpResponseMessage>>(
                       "SendAsync",
                       ItExpr.IsAny<HttpRequestMessage>(),
                       ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(                        // delegate runs per request
                       (HttpRequestMessage _, CancellationToken _) =>
                       {
                           return new HttpResponseMessage(HttpStatusCode.OK)
                           {
                               Content = new StringContent(
                                   $"{{\"success\":{captchaSuccess.ToString().ToLowerInvariant()}}}",
                                   System.Text.Encoding.UTF8,
                                   "application/json")
                           };
                       });

            var httpClient = new HttpClient(handler.Object);
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                       .Returns(httpClient);
        }


        public static HttpContent ScanRequestAsFormData(ScanRequest src)
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("Url",          src.Url),
                new KeyValuePair<string,string>("Email",        src.Email),
                new KeyValuePair<string,string>("cf-turnstile-response", src.CaptchaToken)
            });
        }
    }
}
