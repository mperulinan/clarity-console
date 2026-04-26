using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Repositories;
using Portfolio.Infrastructure.ExternalServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.API.BackgroundServices;

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
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<CoinGeckoProvider>();
builder.Services.AddScoped<IAssetPriceProvider, AssetPriceCacheService>(sp => 
    new AssetPriceCacheService(
        sp.GetRequiredService<CoinGeckoProvider>(), 
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<AssetPriceCacheService>>()
    ));
builder.Services.AddScoped<IAssetCatalogProvider>(sp => sp.GetRequiredService<CoinGeckoProvider>());
builder.Services.AddScoped<IAssetSearchProvider>(sp => sp.GetRequiredService<CoinGeckoProvider>());
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
