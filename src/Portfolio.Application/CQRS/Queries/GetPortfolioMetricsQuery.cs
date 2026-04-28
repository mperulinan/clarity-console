using MediatR;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.CQRS.Queries;

public record GetPortfolioMetricsQuery() : IRequest<PortfolioMetrics>;
