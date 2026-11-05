using MediatR;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Queries;

public class GetAvailableBrokersQueryHandler(IEnumerable<ITransactionImportParser> parsers)
    : IRequestHandler<GetAvailableBrokersQuery, IReadOnlyList<BrokerDto>>
{
    public Task<IReadOnlyList<BrokerDto>> Handle(GetAvailableBrokersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<BrokerDto> brokers = parsers
            .Select(p => new BrokerDto(p.BrokerKey, p.DisplayName))
            .ToList();

        return Task.FromResult(brokers);
    }
}

