using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Queries;

public record GetPortfolioMetricsQuery() : IRequest<PortfolioMetricsDto>;
