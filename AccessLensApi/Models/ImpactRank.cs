namespace AccessLensApi.Models
{
    public static class ImpactPriority
    {
        public static readonly IReadOnlyDictionary<string, int> Rank = new Dictionary<string, int>
        {
            ["critical"] = 0,
            ["serious"] = 1,
            ["moderate"] = 2,
            ["minor"] = 3
        };

        /// <summary>
        /// Helper so callers don’t repeat try-get logic.
        /// Unknown / null impact returns Int32.MaxValue (sorts last).
        /// </summary>
        public static int Get(string? impact) =>
            Rank.TryGetValue(impact ?? string.Empty, out var r) ? r : int.MaxValue;
    }
}
