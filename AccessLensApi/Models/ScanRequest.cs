namespace AccessLensApi.Models
{
    public class ScanRequest
    {
        public string Url { get; set; }
        public string Email { get; set; }
        public string? HcaptchaToken { get; set; }
    }
}
