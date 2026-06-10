using ConsensusService.Commands;
using ConsensusService.Consensus;
using ConsensusService.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace ConsensusService;

/// <summary>
/// Background worker that, once per minute, computes a consensus value from the
/// previous minute's GOOD-quality readings and flags malicious sensors. Reads and
/// writes go through MediatR query/command handlers (CQRS).
/// </summary>
public sealed class ConsensusWorker : BackgroundService
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsensusOptions _options;
    private readonly ILogger<ConsensusWorker> _logger;

    public ConsensusWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ConsensusOptions> options,
        ILogger<ConsensusWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consensus worker started (window: {WindowSeconds}s, grace: {GraceSeconds}s)",
            WindowLength.TotalSeconds,
            _options.GraceSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DelayUntilNextTickAsync(stoppingToken);
                await ProcessWindowAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consensus cycle failed");
            }
        }

        _logger.LogInformation("Consensus worker stopped");
    }

    private async Task ProcessWindowAsync(CancellationToken cancellationToken)
    {
        var windowEnd = FloorToMinute(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(_options.GraceSeconds));
        var windowStart = windowEnd - WindowLength;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        if (await mediator.Send(new ConsensusValueExistsQuery(windowStart), cancellationToken))
        {
            _logger.LogDebug("Window [{Start:HH:mm}-{End:HH:mm}) already processed; skipping", windowStart, windowEnd);
            return;
        }

        var groups = await mediator.Send(
            new GetGoodReadingsForWindowQuery(windowStart, windowEnd),
            cancellationToken);

        if (groups.Count == 0)
        {
            _logger.LogInformation(
                "No GOOD readings in window [{Start:HH:mm}-{End:HH:mm}); skipping",
                windowStart,
                windowEnd);
            return;
        }

        var aggregates = groups
            .Select(g => new SensorAggregate(g.SensorId, OutlierDetector.Median(g.Values), g.Values.Count))
            .ToList();

        var result = OutlierDetector.Detect(aggregates, _options);

        if (result.MajorityGuardTriggered)
        {
            _logger.LogWarning(
                "Outlier rule would flag a majority of {SensorCount} sensors; refusing to flag any",
                aggregates.Count);
        }
        else if (result.Outliers.Count > 0)
        {
            var outlierIds = result.Outliers.Select(o => o.SensorId).ToList();
            _logger.LogWarning(
                "Outliers detected: {OutlierIds} (population median {Median:F2}°C)",
                string.Join(", ", outlierIds),
                OutlierDetector.Median(aggregates.Select(a => a.Median).ToList()));

            await mediator.Send(new MarkSensorsMaliciousCommand(outlierIds), cancellationToken);
        }

        if (result.Honest.Count == 0)
        {
            _logger.LogWarning(
                "No honest sensors remain for window [{Start:HH:mm}-{End:HH:mm}); skipping consensus",
                windowStart,
                windowEnd);
            return;
        }

        var consensus = OutlierDetector.Median(result.Honest.Select(a => a.Median).ToList());
        var sampleCount = result.Honest.Sum(a => a.SampleCount);

        await mediator.Send(
            new InsertConsensusValueCommand(consensus, windowStart, windowEnd, result.Honest.Count, sampleCount),
            cancellationToken);

        _logger.LogInformation(
            "Consensus {Value:F2}°C for [{Start:HH:mm}-{End:HH:mm}) from {SensorCount} sensors / {SampleCount} samples",
            consensus,
            windowStart,
            windowEnd,
            result.Honest.Count,
            sampleCount);
    }

    /// <summary>
    /// Sleeps until the next whole-minute boundary plus the configured grace period.
    /// </summary>
    private async Task DelayUntilNextTickAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var nextBoundary = FloorToMinute(now) + WindowLength + TimeSpan.FromSeconds(_options.GraceSeconds);
        var delay = nextBoundary - now;

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static DateTimeOffset FloorToMinute(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);
}
