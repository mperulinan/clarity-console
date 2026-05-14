using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.API.BackgroundServices;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Infrastructure.ExternalServices;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Repositories;
using Portfolio.Application.CQRS.Queries;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                      });
});

builder.Services.AddDbContext<PortfolioContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Portfolio"));
});

// Dependency Injection
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAssetSearchService, AssetSearchService>();
builder.Services.AddScoped<IInventoryCalculator, InventoryCalculator>();
builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetPortfolioMetricsQuery).Assembly));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<CoinGeckoProvider>();
builder.Services.AddScoped<TwelveDataProvider>();

// ── Price Providers (keyed by AssetType.Value, each wrapped with a caching decorator) ──
builder.Services.AddKeyedScoped<IAssetPriceProvider>(AssetType.Crypto.Value, (sp, _) =>
    new AssetPriceCacheService(
        sp.GetRequiredService<CoinGeckoProvider>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<AssetPriceCacheService>>()));

builder.Services.AddKeyedScoped<IAssetPriceProvider>(AssetType.Stock.Value, (sp, _) =>
    new AssetPriceCacheService(
        sp.GetRequiredService<TwelveDataProvider>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<AssetPriceCacheService>>()));

builder.Services.AddKeyedScoped<IAssetPriceProvider>(AssetType.Index.Value, (sp, _) =>
    new AssetPriceCacheService(
        sp.GetRequiredService<TwelveDataProvider>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<AssetPriceCacheService>>()));

builder.Services.AddScoped<IAssetPriceProviderFactory, KeyedAssetPriceProviderFactory>();

// ── Search Providers (keyed by AssetType.Value) ──
builder.Services.AddKeyedScoped<IAssetSearchProvider>(AssetType.Crypto.Value, (sp, _) => sp.GetRequiredService<CoinGeckoProvider>());
builder.Services.AddKeyedScoped<IAssetSearchProvider>(AssetType.Stock.Value, (sp, _) => sp.GetRequiredService<TwelveDataProvider>());
builder.Services.AddKeyedScoped<IAssetSearchProvider>(AssetType.Index.Value, (sp, _) => sp.GetRequiredService<TwelveDataProvider>());

builder.Services.AddScoped<IAssetCatalogProvider>(sp => sp.GetRequiredService<CoinGeckoProvider>());
builder.Services.AddScoped<IAssetSynchronizationService, AssetSynchronizationService>();

builder.Services.AddScoped<IAssetMarketDataService, AssetMarketDataService>();
builder.Services.AddScoped<IPortfolioMetricsCalculator, PortfolioMetricsCalculator>();
builder.Services.AddHostedService<AssetCatalogSyncBackgroundService>();
builder.Services.AddHttpClient();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
