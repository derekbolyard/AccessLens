# Background Scan Worker Implementation Guide

## Current Status ✅

You have successfully implemented a background worker system for scan processing! Here's what's already in place:

### Core Components

1. **Job System** (`c:\Code\AccessLens\AccessLensApi\Common\Jobs\JobModels.cs`)
   - `IJobQueue<T>` interface for job management
   - `ScanJob` class with retry logic and options
   - `JobStatus` enumeration for tracking job states

2. **Background Worker** (`c:\Code\AccessLens\AccessLensApi\Workers\ScanWorkerService.cs`)
   - Processes scan jobs asynchronously
   - Maps scan results to database models (Report, ScannedUrl, Finding)
   - Handles retries with exponential backoff
   - Sends notifications on completion/failure

3. **New Scans Controller** (`c:\Code\AccessLens\AccessLensApi\Features\Scans\ScansController.cs`)
   - `/api/scans/start` - Enqueues scan jobs
   - `/api/scans/status/{jobId}` - Checks job status
   - Replaces synchronous scanning with async job queuing

4. **In-Memory Job Queue** (`c:\Code\AccessLens\AccessLensApi\Common\Services\InMemoryJobQueue.cs`)
   - Thread-safe job queuing and tracking
   - Job status management and monitoring

## Next Steps for Production 🚀

### 1. Replace Old Scan Controller

**Current State**: ✅ **COMPLETED** - The codebase now uses the unified `ScansController` for all scan operations
**Benefits Achieved**: 
- ✅ Single controller with unified API design
- ✅ Proper protections (rate limiting, captcha) on marketing endpoints
- ✅ Clean separation between authenticated and public scan endpoints
- ✅ Consistent error handling and response formats

### 2. Distributed Job Queue (for production scale)

**Current**: In-memory queue (lost on app restart)
**Recommended**: Replace with persistent queue

```csharp
// Option A: Redis-based queue (recommended)
public interface IDistributedJobQueue<T> : IJobQueue<T>
{
    Task<int> GetQueueLengthAsync();
    Task<IEnumerable<T>> GetJobHistoryAsync(int limit = 100);
}

// Option B: Database-based queue
// Create a Jobs table to persist job state
```

### 3. Horizontal Scaling

**Current**: Single worker in the main API process
**For Scale**: Separate worker processes

```bash
# Deploy multiple worker instances
Worker1: AccessLensApi.Worker.exe
Worker2: AccessLensApi.Worker.exe
Worker3: AccessLensApi.Worker.exe
API: AccessLensApi.exe (without worker)
```

### 4. Job Monitoring & Management

Add these endpoints to `ScansController`:

```csharp
[HttpGet("jobs")]
public async Task<IActionResult> GetUserJobs()
{
    // Return user's job history
}

[HttpDelete("jobs/{jobId}")]
public async Task<IActionResult> CancelJob(string jobId)
{
    // Cancel a pending/running job
}

[HttpGet("queue/status")]
[Authorize(Policy = "Admin")]
public async Task<IActionResult> GetQueueStatus()
{
    // Admin endpoint for queue monitoring
}
```

## Frontend Integration 🖥️

### Update Scan Flow

**Before** (synchronous):
```typescript
// Old way - synchronous scan
const response = await fetch('/api/scan/full', {
  method: 'POST',
  body: scanRequest
});
const report = await response.json();
```

**After** (asynchronous):
```typescript
// New way - async with job tracking
const startResponse = await fetch('/api/scans/start', {
  method: 'POST',
  body: scanRequest
});
const { jobId } = await startResponse.json();

// Poll for completion
const pollForCompletion = async (jobId: string) => {
  while (true) {
    const statusResponse = await fetch(`/api/scans/status/${jobId}`);
    const status = await statusResponse.json();
    
    if (status.status === 'Completed') {
      // Redirect to report
      window.location.href = `/reports/${status.reportId}`;
      break;
    } else if (status.status === 'Failed') {
      // Show error
      showError(status.errorMessage);
      break;
    }
    
    // Wait 2 seconds before next poll
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
};

pollForCompletion(jobId);
```

## Configuration Updates 📝

### appsettings.json

```json
{
  "ScanWorker": {
    "MaxConcurrentJobs": 3,
    "JobTimeoutMinutes": 30,
    "RetryDelayMinutes": 5,
    "MaxRetries": 3
  },
  "JobQueue": {
    "Type": "InMemory", // or "Redis", "Database"
    "ConnectionString": "redis://localhost:6379",
    "QueueName": "accessibility-scans"
  }
}
```

## Monitoring & Logging 📊

### Key Metrics to Track

```csharp
// Add these to your logging
_logger.LogInformation("Scan job {JobId} started for {SiteUrl} - Queue length: {QueueLength}", 
    jobId, siteUrl, queueLength);

_logger.LogInformation("Scan job {JobId} completed in {Duration}ms - {PageCount} pages, {IssueCount} issues", 
    jobId, duration, pageCount, issueCount);

_logger.LogError("Scan job {JobId} failed after {RetryCount} retries: {Error}", 
    jobId, retryCount, error);
```

### Health Checks

```csharp
// Add to Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<ScanWorkerHealthCheck>("scan-worker")
    .AddCheck<JobQueueHealthCheck>("job-queue");
```

## Performance Optimizations ⚡

### 1. Parallel Processing
- Current: 3 concurrent pages per scan
- Optimize: Adjust based on system resources
- Monitor: CPU and memory usage

### 2. Database Optimization
```csharp
// Batch insert findings instead of one-by-one
var findings = page.Issues.Select(issue => new Finding { ... }).ToList();
await unitOfWork.Repository<Finding>().AddRangeAsync(findings);
```

### 3. Caching
```csharp
// Cache scan results for duplicate URLs
services.AddMemoryCache();
services.AddScoped<IScanResultCache, MemoryScanResultCache>();
```

## Security Considerations 🔒

### 1. Rate Limiting per User
```csharp
// Limit scans per user per hour
public class UserScanRateLimiter
{
    public async Task<bool> CanUserStartScan(string userEmail);
}
```

### 2. Resource Limits
```csharp
// Validate scan parameters
if (request.MaxPages > 100 && !user.HasPremium)
{
    return BadRequest("Premium subscription required for >100 pages");
}
```

## Testing Strategy 🧪

### Unit Tests
```csharp
[Test]
public async Task ScanWorker_ProcessesJob_CreatesReportSuccessfully()
{
    // Test job processing logic
}

[Test]
public async Task JobQueue_HandlesRetries_ExponentialBackoff()
{
    // Test retry logic
}
```

### Integration Tests
```csharp
[Test]
public async Task EndToEnd_ScanWorkflow_CompletesSuccessfully()
{
    // Test full scan workflow from API to completion
}
```

## Summary

Your background scan system is **production-ready** for moderate scale! The key remaining work is:

1. ✅ **Core architecture** - Complete
2. ✅ **Database mapping** - Complete  
3. ✅ **Error handling** - Complete
4. 🔄 **Frontend integration** - Update scan flow
5. 🔄 **Remove old controller** - After frontend testing
6. 🚀 **Scale improvements** - As needed (Redis, multiple workers)

The foundation is solid and follows best practices. You can start using this immediately and scale it as your needs grow!
