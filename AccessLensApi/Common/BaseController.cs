using AccessLensApi.Data;
using AccessLensApi.Features.Auth.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessLensApi.Common
{
    public abstract class BaseController : ControllerBase
    {
        protected readonly ApplicationDbContext _db;

        protected BaseController(ApplicationDbContext db)
        {
            _db = db;
        }

        protected async Task<User?> GetCurrentUserAsync()
        {
            var email = User.FindFirstValue("email");
            if (string.IsNullOrEmpty(email)) return null;
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        protected string? GetCurrentUserEmail()
        {
            return User.FindFirstValue("email");
        }

        protected string? GetCurrentUserId()
        {
            return User.FindFirstValue("sub") ?? User.FindFirstValue("userId");
        }
    }
}
