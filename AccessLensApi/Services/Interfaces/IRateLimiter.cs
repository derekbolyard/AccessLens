namespace AccessLensApi.Services.Interfaces
{
    public interface IRateLimiter
    {
        Task<bool> TryAcquireStarterAsync(string ip, string email, bool verified);
        void ReleaseStarter();
        Task<bool> TryAcquireFullScanAsync(string ip, string email);
        void ReleaseFullScan();
    }
}
