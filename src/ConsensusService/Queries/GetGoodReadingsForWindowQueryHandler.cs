using MediatR;
using Microsoft.EntityFrameworkCore;
using SensorMonitoring.Contracts;
using SensorMonitoring.Data;

namespace ConsensusService.Queries;

public sealed class GetGoodReadingsForWindowQueryHandler
    : IRequestHandler<GetGoodReadingsForWindowQuery, IReadOnlyList<SensorWindowReadings>>
{
    private readonly SensorDbContext _db;

    public GetGoodReadingsForWindowQueryHandler(SensorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SensorWindowReadings>> Handle(
        GetGoodReadingsForWindowQuery query,
        CancellationToken cancellationToken)
    {
        // The Timestamp filter uses the (SensorId, Timestamp) index; grouping is
        // done in memory because EF cannot translate a per-group value list to SQL.
        var rows = await _db.SensorReadings
            .AsNoTracking()
            .Where(r => !r.IsConsensus
                && r.Timestamp >= query.WindowStart
                && r.Timestamp < query.WindowEnd
                && r.Sensor.DataQuality == DataQuality.Good)
            .Select(r => new { r.SensorId, r.Value })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.SensorId)
            .Select(g => new SensorWindowReadings(g.Key, g.Select(r => r.Value).ToList()))
            .ToList();
    }
}
