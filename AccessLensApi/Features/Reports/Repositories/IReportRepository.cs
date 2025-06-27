using AccessLensApi.Common;
using AccessLensApi.Common.Repositories;
using AccessLensApi.Features.Reports.Models;

namespace AccessLensApi.Features.Reports.Repositories
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<IEnumerable<Report>> GetReportsByUserEmailAsync(string userEmail);
        Task<PagedResult<Report>> GetReportsByUserEmailPagedAsync(string userEmail, PaginationParams pagination);
        Task<Report?> GetReportWithDetailsAsync(Guid reportId);
        Task<IEnumerable<ScannedUrl>> GetUrlsByReportIdAsync(Guid reportId);
        Task<IEnumerable<Finding>> GetFindingsByReportIdAsync(Guid reportId);
    }
}
