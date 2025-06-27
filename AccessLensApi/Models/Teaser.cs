using AccessLensApi.Models.Unified;

namespace AccessLensApi.Models
{
    public class Teaser
    {
        public string Url { get; set; } = string.Empty;
        public List<AccessibilityTopIssue> TopIssues { get; set; } = [];
    }
}
