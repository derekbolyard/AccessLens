using AccessLensApi.Data;
using AccessLensApi.Models;
using AccessLensApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AccessLensApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> Get([FromQuery] Guid userId)
    {
        var infos = await _db.BrandingInfos
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToListAsync();
        return Ok(infos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] BrandingCreateRequest req)
    {
        var branding = new BrandingInfo
        {
            UserId = req.UserId,
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
        var branding = await _db.BrandingInfos.FindAsync(id);
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
        var branding = await _db.BrandingInfos.FindAsync(id);
        if (branding == null) return NotFound();

        _db.BrandingInfos.Remove(branding);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class BrandingCreateRequest
{
    public Guid UserId { get; set; }
    public string PrimaryColor { get; set; } = "#4f46e5";
    public string SecondaryColor { get; set; } = "#e0e7ff";
    public IFormFile? Logo { get; set; }
}

public class BrandingUpdateRequest
{
    public string PrimaryColor { get; set; } = "#4f46e5";
    public string SecondaryColor { get; set; } = "#e0e7ff";
    public IFormFile? Logo { get; set; }
}
