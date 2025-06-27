namespace AccessLensApi.Common.Utilities
{
    public static class CorrelationIdGenerator
    {
        private static readonly string MachineName = Environment.MachineName;
        private static readonly string ProcessId = Environment.ProcessId.ToString();
        private static long _counter = 0;

        public static string Generate()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var counter = Interlocked.Increment(ref _counter);
            
            // Format: timestamp-machine-process-counter
            return $"{timestamp:x}-{MachineName[..Math.Min(4, MachineName.Length)]}-{ProcessId}-{counter:x}";
        }

        public static string GenerateShort()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }
    }

    public static class HttpContextExtensions
    {
        public static string GetOrCreateCorrelationId(this HttpContext context)
        {
            const string correlationIdKey = "CorrelationId";
            
            if (context.Items.TryGetValue(correlationIdKey, out var existingId) && existingId is string id)
            {
                return id;
            }

            var correlationId = CorrelationIdGenerator.GenerateShort();
            context.Items[correlationIdKey] = correlationId;
            return correlationId;
        }
    }
}
