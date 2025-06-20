namespace AccessLensApi.Config
{
    public record JwtOptions
    {
        public const string Section = "Jwt";
        public string SecretKey { get; init; } = default!;
        public string Issuer { get; init; } = "accesslens";
        public string Audience { get; init; } = "session";
    }

    public record S3Options
    {
        public const string Section = "S3";
        public string Region { get; init; } = "us-east-2";
        public string? ServiceUrl { get; init; }   // null ⇒ real AWS
        public string BucketName { get; init; } = default!;
    }

    public record PlaywrightOptions
    {
        public const string Section = "Playwright";
        public string Browser { get; init; } = "chromium";
        public bool Headless { get; init; } = true;
        public string? BrowsersPath { get; init; }
        public string[] Args { get; init; } = Array.Empty<string>();
    }

    public record MinioOptions
    {
        public const string Section = "Minio";
        public string User { get; init; } = "minioadmin";
        public string Password { get; init; } = string.Empty;
    }
}
