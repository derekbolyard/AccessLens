using System.Text.Json.Nodes;

namespace AccessLensApi.Services
{
    public interface IPdfService
    {
        Task<string> GenerateAndUploadPdf(string siteName, JsonNode json);
    }
}
