
using AccessLensApi.Features.Core.Models;

namespace AccessLensApi.Features.Reports
{
    public interface IReportBuilder
    {
        string RenderHtml(AccessibilityReport model);
        Task<string> GeneratePdfAsync(string html);
    }
}
