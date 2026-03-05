using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Portfolio.Infrastructure.Persistence;

public partial class PortfolioContext : DbContext
{
    public PortfolioContext()
    {
    }

    public PortfolioContext(DbContextOptions<PortfolioContext> options) : base(options)
    {
    }

    public virtual DbSet<Transaction> Transactions { get; set; }
    public virtual DbSet<Asset> Assets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:Portfolio");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__YourTabl__3214EC0704F226F8");

            entity.ToTable("Transaction");

            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.AmountSpent).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.AmountReceived).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FromAssetPriceInUsd).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FromAssetPriceInEur).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.Fee).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FeeAssetPriceInUsd).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FeeAssetPriceInEur).HasColumnType("decimal(36, 18)");
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
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Asset");
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ExternalId).HasMaxLength(200);
            
            entity.Property(e => e.Type)
                .HasColumnName("AssetType")
                .HasMaxLength(50)
                .HasConversion(
                    v => v.Value,
                    v => AssetType.FromValue(v));

            entity.HasData(new List<Asset>
            {
                Asset.CreateForSeeding(FiatCurrency.USD.Id, FiatCurrency.USD.Value, FiatCurrency.USD.Name, FiatCurrency.USD.Value, AssetType.Fiat, FiatCurrency.USD.ImageUrl),
                Asset.CreateForSeeding(FiatCurrency.EUR.Id, FiatCurrency.EUR.Value, FiatCurrency.EUR.Name, FiatCurrency.EUR.Value, AssetType.Fiat, FiatCurrency.EUR.ImageUrl),
            });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
