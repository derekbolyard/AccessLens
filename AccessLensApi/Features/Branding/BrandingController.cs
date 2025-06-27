using AccessLensApi.Common;
using AccessLensApi.Common.Services;
using AccessLensApi.Data;
using AccessLensApi.Features.Core.Models;
using AccessLensApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.IO;
using AccessLensApi.Features.Auth.Models;

namespace AccessLensApi.Features.Branding;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Authenticated")]
public class BrandingController : BaseController
{
    private readonly IFileUploadService _fileUploadService;

    public BrandingController(ApplicationDbContext db, IFileUploadService fileUploadService) : base(db)
    {
        _fileUploadService = fileUploadService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var infos = await _db.BrandingInfos
            .AsNoTracking()
            .Where(b => b.UserId == user.UserId)
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<BrandingInfo>>.SuccessResult(infos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] BrandingCreateRequest req)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var branding = new BrandingInfo
        {
            UserId = user.UserId,
            PrimaryColor = req.PrimaryColor,
            SecondaryColor = req.SecondaryColor
        };

        if (req.Logo != null && req.Logo.Length > 0)
        {
            var uploadOptions = new FileUploadOptions
            {
                MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
                ResizeMaxWidth = 800,
                ResizeMaxHeight = 400
            };

            var uploadResult = await _fileUploadService.UploadImageAsync(req.Logo, "branding", uploadOptions);
            if (!uploadResult.IsSuccess)
            {
                return BadRequest(ApiResponse.ErrorResult(uploadResult.ErrorMessage!));
            }

            branding.LogoUrl = uploadResult.Url!;
        }

        _db.BrandingInfos.Add(branding);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { userId = branding.UserId }, 
            ApiResponse<BrandingInfo>.SuccessResult(branding, "Branding created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromForm] BrandingUpdateRequest req)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var branding = await _db.BrandingInfos.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.UserId);
        if (branding == null) return NotFound();

        branding.PrimaryColor = req.PrimaryColor;
        branding.SecondaryColor = req.SecondaryColor;

        if (req.Logo != null && req.Logo.Length > 0)
        {
            var uploadOptions = new FileUploadOptions
            {
                MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
                ResizeMaxWidth = 800,
                ResizeMaxHeight = 400
            };

            var uploadResult = await _fileUploadService.UploadImageAsync(req.Logo, "branding", uploadOptions);
            if (!uploadResult.IsSuccess)
            {
                return BadRequest(ApiResponse.ErrorResult(uploadResult.ErrorMessage!));
            }

            branding.LogoUrl = uploadResult.Url!;
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResult("Branding updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var branding = await _db.BrandingInfos.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.UserId);
        if (branding == null) return NotFound(ApiResponse.ErrorResult("Branding not found"));

        _db.BrandingInfos.Remove(branding);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.SuccessResult("Branding deleted successfully"));
    }
}

public class BrandingCreateRequest
{
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string PrimaryColor { get; set; } = "#4f46e5";
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string SecondaryColor { get; set; } = "#e0e7ff";
    public IFormFile? Logo { get; set; }
}

public class BrandingUpdateRequest
{
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string PrimaryColor { get; set; } = "#4f46e5";
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string SecondaryColor { get; set; } = "#e0e7ff";
    public IFormFile? Logo { get; set; }
}
