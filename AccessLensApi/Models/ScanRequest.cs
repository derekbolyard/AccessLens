namespace AccessLensApi.Models
{
    /// <summary>
    /// Body payload for POST /api/scan/starter
    /// </summary>
    public sealed record ScanRequest(string Url);
}
