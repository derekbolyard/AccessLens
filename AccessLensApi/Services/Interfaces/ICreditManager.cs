using System.Threading.Tasks;

namespace AccessLensApi.Services.Interfaces
{
    public interface ICreditManager
    {
        /// <summary>
        /// Returns true if the user has an active subscription OR remaining snapshot credits.
        /// If using snapshot credit, this should decrement credits by one atomically.
        /// </summary>
        Task<bool> HasQuotaAsync(string email);

        /// <summary>
        /// Returns true if the user has premium access (active subscription) for full site scanning.
        /// Does not consume credits - this is just a check.
        /// </summary>
        Task<bool> HasPremiumAccessAsync(string email);
    }
}
