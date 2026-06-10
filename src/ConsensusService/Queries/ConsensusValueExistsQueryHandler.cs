using MediatR;
using Microsoft.EntityFrameworkCore;
using SensorMonitoring.Data;

namespace ConsensusService.Queries;

public sealed class ConsensusValueExistsQueryHandler : IRequestHandler<ConsensusValueExistsQuery, bool>
{
    private readonly SensorDbContext _db;

    public ConsensusValueExistsQueryHandler(SensorDbContext db)
    {
        _db = db;
    }

    public Task<bool> Handle(ConsensusValueExistsQuery query, CancellationToken cancellationToken)
    {
        return _db.ConsensusValues
            .AsNoTracking()
            .AnyAsync(c => c.PeriodStart == query.PeriodStart, cancellationToken);
    }
}
