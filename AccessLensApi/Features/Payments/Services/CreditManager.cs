using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AccessLensApi.Data;
using AccessLensApi.Features.Core.Interfaces;
using System.Data;
using Dapper;
using System.Data.Common;

namespace AccessLensApi.Features.Payments.Services
{
    public class CreditManager : ICreditManager
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbConnection _dbConnection;

        public CreditManager(ApplicationDbContext dbContext, IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbConnection = dbConnection as DbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        }

        public async Task<bool> HasQuotaAsync(string email)
        {
            // 1) Check active subscription via EF
            var hasSub = await _dbContext.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.Email == email && s.Active);

            if (hasSub)
                return true;

            if (_dbConnection.State == ConnectionState.Closed)
                await _dbConnection.OpenAsync();
            const string sql = @"
                    UPDATE SnapshotPasses
                    SET    CreditsLeft = CreditsLeft - 1,
                            UpdatedAt   = CURRENT_TIMESTAMP
                    WHERE  Email = @Email
                        AND  CreditsLeft > 0
                    RETURNING CreditsLeft;
            ";

            var oldCredits = await _dbConnection.QuerySingleOrDefaultAsync<int?>(sql, new { Email = email });

            return oldCredits.HasValue;
        }

        public async Task<bool> HasPremiumAccessAsync(string email)
        {
            // Only active subscriptions allow full site scanning
            // Snapshot passes are for single-page scans only
            return await _dbContext.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.Email == email && s.Active);
        }
    }
}