using System.Text.Json.Nodes;

namespace AccessLensApi.Services
{
    public interface IPdfService
    {
        byte[] GeneratePdf(string siteName, JsonNode json);
    }
}
