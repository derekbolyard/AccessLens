public record PlaywrightOptions
{
    public bool Headless { get; init; } = true;
    public string[] Args { get; init; } = Array.Empty<string>();
    public string Browser { get; init; } = "chromium";   // future-proof
}
