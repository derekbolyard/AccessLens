using AccessLensApi.Data;
using AccessLensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace AccessLensApi.Features.Reports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Authenticated")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _db.Reports
                .Include(r => r.ScannedUrls)
                .Include(r => r.Findings)
                .AsNoTracking()
                .ToListAsync();
            return Ok(reports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var report = await _db.Reports
                .Include(r => r.ScannedUrls)
                .Include(r => r.Findings)
                .FirstOrDefaultAsync(r => r.ReportId == id);
            if (report == null) return NotFound();
            return Ok(report);
        }

        [HttpGet("{id}/urls")]
        public async Task<IActionResult> GetUrls(Guid id)
        {
            var urls = await _db.ScannedUrls
                .AsNoTracking()
                .Where(u => u.ReportId == id)
                .ToListAsync();
            return Ok(urls);
        }

        [HttpGet("{id}/findings")]
        public async Task<IActionResult> GetFindings(Guid id)
        {
            var findings = await _db.Findings
                .AsNoTracking()
                .Where(f => f.ReportId == id)
                .ToListAsync();
            return Ok(findings);
        }
    }
}
