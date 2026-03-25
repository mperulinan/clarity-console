using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> entity)
    {
        entity.ToTable("Asset");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.ExternalId).HasMaxLength(200);

        entity.Property(e => e.Type)
            .HasColumnName("AssetType")
            .HasMaxLength(50)
            .HasConversion(
                v => v.Value,
                v => AssetType.FromValue(v));

        entity.HasData(
            Asset.CreateForSeeding(FiatCurrency.USD.Id, FiatCurrency.USD.Symbol, FiatCurrency.USD.Name, FiatCurrency.USD.Value, AssetType.Fiat, FiatCurrency.USD.ImageUrl),
            Asset.CreateForSeeding(FiatCurrency.EUR.Id, FiatCurrency.EUR.Symbol, FiatCurrency.EUR.Name, FiatCurrency.EUR.Value, AssetType.Fiat, FiatCurrency.EUR.ImageUrl)
        );
    }
}
