using MediatR;

namespace ConsensusService.Queries;

/// <summary>
/// All values reported by a single GOOD-quality sensor within a consensus window.
/// </summary>
public sealed record SensorWindowReadings(string SensorId, IReadOnlyList<double> Values);

/// <summary>
/// Reads the raw (non-consensus) readings of GOOD-quality sensors in the
/// half-open window [WindowStart, WindowEnd), grouped per sensor.
/// </summary>
public sealed record GetGoodReadingsForWindowQuery(DateTimeOffset WindowStart, DateTimeOffset WindowEnd)
    : IRequest<IReadOnlyList<SensorWindowReadings>>;
