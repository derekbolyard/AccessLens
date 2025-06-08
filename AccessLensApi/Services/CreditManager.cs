using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using AccessLensApi.Data;
using AccessLensApi.Services.Interfaces;

namespace AccessLensApi.Services
{
    public class CreditManager : ICreditManager
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDbConnection _dbConnection;

        public CreditManager(ApplicationDbContext dbContext, IDbConnection dbConnection)
        {
            _dbContext = dbContext;
            _dbConnection = dbConnection;
        }

        public async Task<bool> HasQuotaAsync(string email)
        {
            // 1) Check active subscription via EF
            var hasSub = await _dbContext.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.Email == email && s.Active);

            if (hasSub)
                return true;

            // 2) Attempt atomic decrement of one SnapshotPass credit
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
    }
}
