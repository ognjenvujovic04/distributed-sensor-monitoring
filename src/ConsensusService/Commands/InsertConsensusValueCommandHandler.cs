using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SensorMonitoring.Data;
using SensorMonitoring.Data.Entities;

namespace ConsensusService.Commands;

public sealed class InsertConsensusValueCommandHandler : IRequestHandler<InsertConsensusValueCommand>
{
    private const string UniqueViolation = "23505";

    private readonly SensorDbContext _db;
    private readonly ILogger<InsertConsensusValueCommandHandler> _logger;

    public InsertConsensusValueCommandHandler(
        SensorDbContext db,
        ILogger<InsertConsensusValueCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(InsertConsensusValueCommand command, CancellationToken cancellationToken)
    {
        _db.ConsensusValues.Add(new ConsensusValue
        {
            CalculatedValue = command.CalculatedValue,
            PeriodStart = command.PeriodStart,
            PeriodEnd = command.PeriodEnd,
            Timestamp = DateTimeOffset.UtcNow,
            SensorCount = command.SensorCount,
            SampleCount = command.SampleCount
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            // Another run already wrote this window; the unique PeriodStart index protected us.
            _logger.LogInformation(
                "Consensus value for window starting {PeriodStart} already exists; skipping",
                command.PeriodStart);
        }
    }
}
