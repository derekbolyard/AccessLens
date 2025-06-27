using AccessLensApi.Features.Core.Models;

namespace AccessLensApi.Features.Scans.Models
{
    public class Teaser
    {
        public string Url { get; set; } = string.Empty;
        public List<AccessibilityTopIssue> TopIssues { get; set; } = [];
    }
}
