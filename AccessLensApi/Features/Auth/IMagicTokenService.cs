namespace AccessLensApi.Features.Auth
{
    public interface IMagicTokenService
    {
        string BuildMagicToken(string email);
        string BuildSessionToken(string email);
    }
}
