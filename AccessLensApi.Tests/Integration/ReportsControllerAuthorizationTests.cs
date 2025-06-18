using AccessLensApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace AccessLensApi.Tests.Integration
{
    public class ReportsControllerAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ReportsControllerAuthorizationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();

                    services.AddDbContext<ApplicationDbContext>(o =>
                        o.UseSqlite(connection));
                });
            }).CreateClient();
        }

        [Theory]
        [InlineData("/api/reports")]
        [InlineData("/api/reports/00000000-0000-0000-0000-000000000000")]
        [InlineData("/api/reports/00000000-0000-0000-0000-000000000000/urls")]
        [InlineData("/api/reports/00000000-0000-0000-0000-000000000000/findings")]
        public async Task Endpoints_RequireAuthentication(string url)
        {
            var response = await _client.GetAsync(url);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
