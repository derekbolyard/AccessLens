using System.Threading.Tasks;

namespace AccessLensApi.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Send the 6-digit verification code to the specified email.
        /// </summary>
        Task SendVerificationCodeAsync(string email, string code);

        /// <summary>
        /// Send the final PDF report email (with presigned link and score/thumbnail).
        /// </summary>
        Task SendScanResultEmailAsync(string email, string presignedPdfUrl, int score, string presignedTeaserUrl = null);
    }
}