namespace AccessLensApi.Features.Scans.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using AccessLensApi.Features.Scans.Services; // For PageScanResult

    public record A11yScanResult(
        [property: JsonPropertyName("pages")] List<PageResult> Pages,
        [property: JsonPropertyName("pageScans")] List<PageScanResult> PageScans, // NEW: All scan attempts (success + failure)
        [property: JsonPropertyName("teaser")] TeaserDto? Teaser,
        [property: JsonPropertyName("totalPages")] int TotalPages,
        [property: JsonPropertyName("successfulPages")] int SuccessfulPages,      // NEW: Count of successful scans
        [property: JsonPropertyName("failedPages")] int FailedPages,              // NEW: Count of failed scans
        [property: JsonPropertyName("scannedAt")] DateTime ScannedAtUtc,
        [property: JsonPropertyName("discoveryMethod")]
                                               string DiscoveryMethod);

    public record PageResult(
        [property: JsonPropertyName("pageUrl")] string PageUrl,
        [property: JsonPropertyName("issues")] List<Issue> Issues);

    public record Issue(
        [property: JsonPropertyName("type")] string Type,   // critical | serious | …
        [property: JsonPropertyName("code")] string Code,   // e.g. color-contrast
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("context")] string ContextHtml);

    public record TeaserDto(
        [property: JsonPropertyName("url")] string? Url = null,
        [property: JsonPropertyName("topIssues")] List<TopIssue>? TopIssues = null);

    public record TopIssue(
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("text")] string Text);
}
