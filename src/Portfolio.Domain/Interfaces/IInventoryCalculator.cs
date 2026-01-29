using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface IInventoryCalculator
{
    PortfolioReport CalculateInventory(IEnumerable<Transaction> transactions, FiatCurrency currency);
}
