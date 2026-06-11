using MediatR;

namespace ConsensusService.Queries;

/// <summary>
/// Checks whether a consensus value has already been written for the given window
/// start, guarding against duplicate processing of the same minute.
/// </summary>
public sealed record ConsensusValueExistsQuery(DateTimeOffset PeriodStart) : IRequest<bool>;
