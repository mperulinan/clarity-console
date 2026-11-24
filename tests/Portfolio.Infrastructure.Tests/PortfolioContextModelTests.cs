using Microsoft.EntityFrameworkCore;
using Portfolio.Infrastructure.Persistence;
using Xunit;

namespace Portfolio.Infrastructure.Tests;

public class PortfolioContextModelTests
{
    [Fact]
    public void ModelValidation_ShouldSucceed()
    {
        var options = new DbContextOptionsBuilder<PortfolioContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Integrated Security=true;TrustServerCertificate=true")
            .Options;

        using var context = new PortfolioContext(options);
        var model = context.Model;
        Assert.NotNull(model);
    }
}
