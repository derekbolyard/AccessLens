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
    }
}
