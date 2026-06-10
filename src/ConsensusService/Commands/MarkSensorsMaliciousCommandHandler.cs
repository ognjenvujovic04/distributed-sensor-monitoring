using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SensorMonitoring.Contracts;
using SensorMonitoring.Data;

namespace ConsensusService.Commands;

public sealed class MarkSensorsMaliciousCommandHandler : IRequestHandler<MarkSensorsMaliciousCommand>
{
    private readonly SensorDbContext _db;
    private readonly ILogger<MarkSensorsMaliciousCommandHandler> _logger;

    public MarkSensorsMaliciousCommandHandler(
        SensorDbContext db,
        ILogger<MarkSensorsMaliciousCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(MarkSensorsMaliciousCommand command, CancellationToken cancellationToken)
    {
        // Only touch sensors still GOOD, which keeps the operation idempotent.
        var sensors = await _db.Sensors
            .Where(s => command.SensorIds.Contains(s.Id) && s.DataQuality == DataQuality.Good)
            .ToListAsync(cancellationToken);

        if (sensors.Count == 0)
        {
            return;
        }

        foreach (var sensor in sensors)
        {
            sensor.DataQuality = DataQuality.Bad;
            _logger.LogWarning("Sensor {SensorId} marked MALICIOUS (DataQuality=Bad)", sensor.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
