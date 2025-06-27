namespace AccessLensApi.Features.Core.Interfaces
{
    public interface IAxeScriptProvider
    {
        Task<string> GetAsync(CancellationToken ct = default);
    }
}
