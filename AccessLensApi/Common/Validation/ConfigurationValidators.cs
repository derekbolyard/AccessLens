using AccessLensApi.Config;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Common.Validation
{
    public class JwtOptionsValidator : IValidateOptions<JwtOptions>
    {
        public ValidateOptionsResult Validate(string? name, JwtOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.SecretKey))
                failures.Add("JWT SecretKey is required");

            if (options.SecretKey?.Length < 32)
                failures.Add("JWT SecretKey must be at least 32 characters long");

            if (string.IsNullOrWhiteSpace(options.Issuer))
                failures.Add("JWT Issuer is required");

            if (string.IsNullOrWhiteSpace(options.Audience))
                failures.Add("JWT Audience is required");

            return failures.Count > 0 
                ? ValidateOptionsResult.Fail(failures)
                : ValidateOptionsResult.Success;
        }
    }

    public class S3OptionsValidator : IValidateOptions<S3Options>
    {
        public ValidateOptionsResult Validate(string? name, S3Options options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.Region))
                failures.Add("S3 Region is required");

            if (string.IsNullOrWhiteSpace(options.BucketName))
                failures.Add("S3 BucketName is required");

            return failures.Count > 0 
                ? ValidateOptionsResult.Fail(failures)
                : ValidateOptionsResult.Success;
        }
    }

    public class PlaywrightOptionsValidator : IValidateOptions<AccessLensApi.Config.PlaywrightOptions>
    {
        public ValidateOptionsResult Validate(string? name, AccessLensApi.Config.PlaywrightOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.Browser))
                failures.Add("Playwright Browser is required");

            var validBrowsers = new[] { "chromium", "firefox", "webkit" };
            if (!validBrowsers.Contains(options.Browser.ToLower()))
                failures.Add($"Playwright Browser must be one of: {string.Join(", ", validBrowsers)}");

            return failures.Count > 0 
                ? ValidateOptionsResult.Fail(failures)
                : ValidateOptionsResult.Success;
        }
    }
}
