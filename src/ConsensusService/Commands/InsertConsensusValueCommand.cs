using MediatR;

namespace ConsensusService.Commands;

/// <summary>
/// Persists a calculated consensus value for one completed minute window,
/// together with the number of participating sensors and samples.
/// </summary>
public sealed record InsertConsensusValueCommand(
    double CalculatedValue,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int SensorCount,
    int SampleCount) : IRequest;
