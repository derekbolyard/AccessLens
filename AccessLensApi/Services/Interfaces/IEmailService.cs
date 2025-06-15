using System.Threading.Tasks;

namespace AccessLensApi.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Send a magic link JWT token to the specified email.
        /// </summary>
        Task SendMagicLinkAsync(string email, string magicToken);

        /// <summary>
        /// Send the final PDF report email (with presigned link and score/thumbnail).
        /// </summary>
        Task SendScanResultEmailAsync(string email, string presignedPdfUrl, int score, string presignedTeaserUrl = null);
    }
}