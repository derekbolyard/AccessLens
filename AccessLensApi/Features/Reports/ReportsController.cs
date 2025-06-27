using AccessLensApi.Common;
using AccessLensApi.Common.Repositories;
using AccessLensApi.Data;
using AccessLensApi.Features.Reports.Models;
using AccessLensApi.Features.Reports.Repositories;
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
    public class ReportsController : BaseController
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReportsController(ApplicationDbContext db, IReportRepository reportRepository, IUnitOfWork unitOfWork) 
            : base(db)
        {
            _reportRepository = reportRepository;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            var pagedReports = await _reportRepository.GetReportsByUserEmailPagedAsync(userEmail, pagination);
            return Ok(ApiResponse<PagedResult<Models.Report>>.SuccessResult(pagedReports));
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllList()
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            var reports = await _reportRepository.GetReportsByUserEmailAsync(userEmail);
            return Ok(ApiResponse<IEnumerable<Models.Report>>.SuccessResult(reports));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var report = await _reportRepository.GetReportWithDetailsAsync(id);
            if (report == null) 
                return NotFound(ApiResponse<Models.Report>.ErrorResult("Report not found"));
                
            return Ok(ApiResponse<Models.Report>.SuccessResult(report));
        }

        [HttpGet("{id}/urls")]
        public async Task<IActionResult> GetUrls(Guid id)
        {
            var urls = await _reportRepository.GetUrlsByReportIdAsync(id);
            return Ok(ApiResponse<IEnumerable<Models.ScannedUrl>>.SuccessResult(urls));
        }

        [HttpGet("{id}/findings")]
        public async Task<IActionResult> GetFindings(Guid id)
        {
            var findings = await _reportRepository.GetFindingsByReportIdAsync(id);
            return Ok(ApiResponse<IEnumerable<Models.Finding>>.SuccessResult(findings));
        }
    }
}
