using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;

namespace AccessLensApi.Tests.Helpers
{
    public class FakeHttpHandler
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        private readonly Mock<HttpMessageHandler> _handler;

        public FakeHttpHandler(IServiceCollection services)
        {
            _handler = new Mock<HttpMessageHandler>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var httpClient = new HttpClient(_handler.Object);

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton(_mockHttpClientFactory.Object);
        }

        /// <summary>
        /// Suitable when all calls should return the same response.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="response"></param>
        public void SetupResponse(HttpStatusCode statusCode, object response)
        {
            _handler.Protected()
                 .Setup<Task<HttpResponseMessage>>(
                     "SendAsync",
                     ItExpr.IsAny<HttpRequestMessage>(),
                     ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     StatusCode = HttpStatusCode.OK,
                     Content = new StringContent(JsonConvert.SerializeObject(response))
                 });
        }
    }
}
