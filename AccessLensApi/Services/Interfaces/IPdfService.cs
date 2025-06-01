using System.Text.Json.Nodes;

namespace AccessLensApi.Services.Interfaces
{
    public interface IPdfService
    {
        Task<string> GenerateAndUploadPdf(string siteName, JsonNode json);
    }
}
