using MediatR;

namespace ConsensusService.Commands;

/// <summary>
/// Marks the given sensors as malicious by setting their data quality to BAD.
/// The change is permanent; a malicious sensor is never auto-recovered.
/// </summary>
public sealed record MarkSensorsMaliciousCommand(IReadOnlyCollection<string> SensorIds) : IRequest;
