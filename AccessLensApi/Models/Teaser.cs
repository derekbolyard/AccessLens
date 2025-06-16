using AccessLensApi.Features.Scans.Models;

namespace AccessLensApi.Models
{
    public class Teaser
    {
        public string Url { get; set; } = string.Empty;
        public List<TopIssue> TopIssues { get; set; } = [];
    }
}
