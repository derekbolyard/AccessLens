namespace AccessLensApi.Features.Auth.Models
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string HcaptchaToken { get; set; } = string.Empty;
    }
}
