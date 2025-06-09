using AccessLensApi.Data;
using AccessLensApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AccessLensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var reports = await _db.Reports.AsNoTracking().ToListAsync();
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
    }
}
