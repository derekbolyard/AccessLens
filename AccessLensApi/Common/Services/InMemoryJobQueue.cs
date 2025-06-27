using AccessLensApi.Common.Jobs;
using System.Collections.Concurrent;

namespace AccessLensApi.Common.Services
{
    public class InMemoryJobQueue<T> : IJobQueue<T> where T : JobBase
    {
        private readonly ConcurrentQueue<T> _jobs = new();
        private readonly ConcurrentDictionary<string, T> _jobTracker = new();
        private readonly ILogger<InMemoryJobQueue<T>> _logger;

        public InMemoryJobQueue(ILogger<InMemoryJobQueue<T>> logger)
        {
            _logger = logger;
        }

        public Task EnqueueAsync(T job)
        {
            job.Status = JobStatus.Pending;
            job.CreatedAt = DateTime.UtcNow;
            
            _jobs.Enqueue(job);
            _jobTracker.TryAdd(job.Id, job);
            
            _logger.LogInformation("Job {JobId} of type {JobType} enqueued", job.Id, typeof(T).Name);
            return Task.CompletedTask;
        }

        public Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_jobs.TryDequeue(out var job))
            {
                job.Status = JobStatus.Processing;
                job.StartedAt = DateTime.UtcNow;
                _jobTracker.TryUpdate(job.Id, job, job);
                
                _logger.LogInformation("Job {JobId} dequeued for processing", job.Id);
                return Task.FromResult<T?>(job);
            }

            return Task.FromResult<T?>(null);
        }

        public Task<IEnumerable<T>> GetPendingJobsAsync()
        {
            var pendingJobs = _jobTracker.Values
                .Where(j => j.Status == JobStatus.Pending)
                .OrderBy(j => j.CreatedAt)
                .AsEnumerable();
            
            return Task.FromResult(pendingJobs);
        }

        public Task MarkJobCompletedAsync(string jobId)
        {
            if (_jobTracker.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _jobTracker.TryUpdate(jobId, job, job);
                
                _logger.LogInformation("Job {JobId} marked as completed", jobId);
            }

            return Task.CompletedTask;
        }

        public Task MarkJobFailedAsync(string jobId, string errorMessage)
        {
            if (_jobTracker.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = errorMessage;
                job.CompletedAt = DateTime.UtcNow;
                job.RetryCount++;
                
                _jobTracker.TryUpdate(jobId, job, job);
                
                _logger.LogError("Job {JobId} marked as failed: {ErrorMessage}", jobId, errorMessage);
            }

            return Task.CompletedTask;
        }

        public Task MarkJobStartedAsync(string jobId)
        {
            if (_jobTracker.TryGetValue(jobId, out var job))
            {
                job.Status = JobStatus.Processing;
                job.StartedAt = DateTime.UtcNow;
                _jobTracker.TryUpdate(jobId, job, job);
                
                _logger.LogInformation("Job {JobId} marked as started", jobId);
            }

            return Task.CompletedTask;
        }

        public T? GetJobStatus(string jobId)
        {
            _jobTracker.TryGetValue(jobId, out var job);
            return job;
        }
    }
}
