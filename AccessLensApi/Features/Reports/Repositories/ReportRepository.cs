using AccessLensApi.Common;
using AccessLensApi.Common.Repositories;
using AccessLensApi.Data;
using AccessLensApi.Features.Reports.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessLensApi.Features.Reports.Repositories
{
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Report>> GetReportsByUserEmailAsync(string userEmail)
        {
            return await _dbSet
                .Where(r => r.Email == userEmail)
                .Include(r => r.ScannedUrls)
                .Include(r => r.Findings)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Report?> GetReportWithDetailsAsync(Guid reportId)
        {
            return await _dbSet
                .Include(r => r.ScannedUrls)
                .Include(r => r.Findings)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<IEnumerable<ScannedUrl>> GetUrlsByReportIdAsync(Guid reportId)
        {
            return await _context.ScannedUrls
                .AsNoTracking()
                .Where(u => u.ReportId == reportId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Finding>> GetFindingsByReportIdAsync(Guid reportId)
        {
            return await _context.Findings
                .AsNoTracking()
                .Where(f => f.ReportId == reportId)
                .ToListAsync();
        }

        public async Task<PagedResult<Report>> GetReportsByUserEmailPagedAsync(string userEmail, PaginationParams pagination)
        {
            var query = _dbSet
                .Where(r => r.Email == userEmail)
                .Include(r => r.ScannedUrls)
                .Include(r => r.Findings)
                .AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(pagination.Search))
            {
                query = query.Where(r => r.SiteName.Contains(pagination.Search) || r.Status.Contains(pagination.Search));
            }

            // Apply sorting
            query = pagination.SortBy?.ToLower() switch
            {
                "sitename" => pagination.SortDescending ? query.OrderByDescending(r => r.SiteName) : query.OrderBy(r => r.SiteName),
                "scandate" => pagination.SortDescending ? query.OrderByDescending(r => r.ScanDate) : query.OrderBy(r => r.ScanDate),
                "status" => pagination.SortDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                _ => query.OrderByDescending(r => r.ScanDate) // Default sort by scan date
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<Report>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }
    }
}
