namespace AccessLensApi.Services.Interfaces
{
    public interface IAxeScriptProvider
    {
        Task<string> GetAsync(CancellationToken ct = default);
    }
}
