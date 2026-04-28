using MediatR;

namespace Portfolio.Application.CQRS.Commands;

public record CalculateExchangeRatesCommand() : IRequest;
