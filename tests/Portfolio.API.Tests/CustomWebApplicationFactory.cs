using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Portfolio.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Portfolio.API.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configuration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PortfolioContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add the Testcontainers DbContext configuration
            services.AddDbContext<PortfolioContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });

            // Ensure schema is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
            db.Database.Migrate();
        });
    }

    /// <summary>
    /// Returns the same <see cref="JsonSerializerOptions"/> that ASP.NET Core uses when
    /// serializing/deserializing controller responses.  Tests must use this when calling
    /// <c>PostAsJsonAsync</c> or <c>ReadFromJsonAsync</c> so that SmartEnum converters
    /// (e.g. <c>TransactionType</c>) are handled identically on both sides.
    /// </summary>
    public JsonSerializerOptions GetJsonOptions()
    {
        using var scope = Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<JsonOptions>>();
        return options.Value.JsonSerializerOptions;
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}

// Shared collection definition so all controller test classes share ONE factory instance
// (and therefore one DB container) while still running in the same process.
[CollectionDefinition("API collection")]
public class ApiCollectionDefinition : ICollectionFixture<CustomWebApplicationFactory<Program>> { }
