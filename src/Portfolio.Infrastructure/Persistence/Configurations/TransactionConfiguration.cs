using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.ToTable("Transaction");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Date).HasColumnType("datetime");
        entity.Property(e => e.AmountSpent).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.AmountReceived).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.SpotPriceUSD).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.SpotPriceEUR).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.Fee).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.FeePriceUSD).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.FeePriceEUR).HasColumnType("decimal(36, 18)");
        entity.Property(e => e.UsdEurExchangeRate).HasColumnType("decimal(18, 8)");
        entity.Property(e => e.Notes).HasMaxLength(-1);

        entity.HasOne(d => d.FromAsset)
            .WithMany()
            .HasForeignKey(d => d.FromAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Transaction_FromAsset");

        entity.HasOne(d => d.ToAsset)
            .WithMany()
            .HasForeignKey(d => d.ToAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Transaction_ToAsset");

        entity.HasOne(d => d.FeeAsset)
            .WithMany()
            .HasForeignKey(d => d.FeeAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Transaction_FeeAsset");

        entity.Property(e => e.Type)
            .HasColumnName("TransactionType")
            .HasMaxLength(50)
            .HasConversion(
                v => v.Value,
                v => TransactionType.FromValue(v));

        entity.Property(e => e.SpotPriceInputCurrency)
            .HasMaxLength(10)
            .HasConversion(
                v => v.Value,
                v => FiatCurrency.FromValue(v));

        entity.Property(e => e.FeePriceInputCurrency)
            .HasMaxLength(10)
            .HasConversion(
                v => v == null ? null : v.Value,
                v => v == null ? null : FiatCurrency.FromValue(v));
    }
}
