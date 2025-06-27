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

namespace AccessLensApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Authenticated")]
public class BrandingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;

    public BrandingController(ApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
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
        return Ok(infos);
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
            var ext = Path.GetExtension(req.Logo.FileName);
            var key = $"branding/{branding.Id}{ext}";
            using var ms = new MemoryStream();
            await req.Logo.CopyToAsync(ms);
            await _storage.UploadAsync(key, ms.ToArray());
            branding.LogoUrl = _storage.GetPresignedUrl(key, TimeSpan.FromDays(365));
        }

        _db.BrandingInfos.Add(branding);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { userId = branding.UserId }, branding);
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
            var ext = Path.GetExtension(req.Logo.FileName);
            var key = $"branding/{branding.Id}{ext}";
            using var ms = new MemoryStream();
            await req.Logo.CopyToAsync(ms);
            await _storage.UploadAsync(key, ms.ToArray());
            branding.LogoUrl = _storage.GetPresignedUrl(key, TimeSpan.FromDays(365));
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var branding = await _db.BrandingInfos.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.UserId);
        if (branding == null) return NotFound();

        _db.BrandingInfos.Remove(branding);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var email = User.FindFirstValue("email");
        if (string.IsNullOrEmpty(email)) return null;
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
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
