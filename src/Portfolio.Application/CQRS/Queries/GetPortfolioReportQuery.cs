using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Queries;

public record GetPortfolioReportQuery() : IRequest<PortfolioReportDto>;
