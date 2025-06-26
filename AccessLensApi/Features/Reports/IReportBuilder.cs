
using AccessLensApi.Features.Reports.Models;

namespace AccessLensApi.Features.Reports
{
    public interface IReportBuilder
    {
        string RenderHtml(AccessibilityReport model);
        Task<string> GeneratePdfAsync(string html);
    }
}
