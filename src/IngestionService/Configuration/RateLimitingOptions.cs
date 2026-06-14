namespace IngestionService.Configuration
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";
        public int PermitLimit { get; set; } = 10;
        public int WindowSeconds { get; set; } = 1;
    }
}
