using System.Collections.Concurrent;

namespace IngestionService.Services
{
    public class SensorBlockCoordinator
    {
        private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<string, DateTimeOffset> _lastBlockedAt = new(StringComparer.Ordinal);

        public bool ShouldBlock(string sensorId)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var blocked = false;

            _lastBlockedAt.AddOrUpdate(
                sensorId,
                _ =>
                {
                    blocked = true;
                    return utcNow;
                },
                (_, lastBlockedAt) =>
                {
                    if (utcNow - lastBlockedAt < DebounceWindow)
                    {
                        return lastBlockedAt;
                    }

                    blocked = true;
                    return utcNow;
                });

            return blocked;
        }
    }
}
