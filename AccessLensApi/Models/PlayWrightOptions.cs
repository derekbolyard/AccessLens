public record PlaywrightOptions
{
    public string Section { get; set; } = "Playwright";
    public bool Headless { get; init; } = true;
    public string[] Args { get; init; } = Array.Empty<string>();
    public string Browser { get; init; } = "chromium";   // future-proof
}
