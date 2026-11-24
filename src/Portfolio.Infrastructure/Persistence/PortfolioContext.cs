using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence;

public class PortfolioContext(DbContextOptions<PortfolioContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Asset> Assets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Optional owned Price (FeePrice). All columns nullable. No own key. Known EF warning.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.OptionalDependentWithoutIdentifyingPropertyWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioContext).Assembly);
    }
}
