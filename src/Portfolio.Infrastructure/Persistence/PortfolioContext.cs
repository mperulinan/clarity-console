using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
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
    public virtual DbSet<TransactionType> TransactionTypes { get; set; }
    public virtual DbSet<Asset> Assets { get; set; }
    public virtual DbSet<AssetType> AssetTypes { get; set; }

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
            entity.Property(e => e.FromAssetId).HasMaxLength(50);
            entity.Property(e => e.ToAssetId).HasMaxLength(50);
            entity.Property(e => e.AmountSpent).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.AmountReceived).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FromAssetPriceInUsd).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FromAssetPriceInEur).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.Fee).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FeeAsset).HasMaxLength(50);
            entity.Property(e => e.FeeAssetPriceInUsd).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FeeAssetPriceInEur).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.UsdEurExchangeRate).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.Notes).HasMaxLength(-1);

            // Map TransactionTypeCode property to the "TransactionType" column
            entity.Property(e => e.TransactionTypeCode)
                .HasColumnName("TransactionType")
                .HasMaxLength(50);

            entity.HasOne(d => d.TransactionType)
                .WithMany() // Assuming TransactionType doesn't need a collection of Transactions back for now, or use .WithMany("Transactions") if it exists
                .HasForeignKey(d => d.TransactionTypeCode)
                .HasPrincipalKey(tt => tt.Code) // The String PK in the lookup table
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transaction_TransactionType");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Asset");
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ExternalId).HasMaxLength(200);
            entity.Property(e => e.AssetTypeCode).HasMaxLength(50);

            entity.HasOne(d => d.AssetType)
                .WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.Value);
            entity.ToTable("AssetType");
            entity.Property(e => e.Value).HasColumnName("Code").HasMaxLength(50).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

            entity.HasData(AssetType.List);
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.Code);

            entity.ToTable("TransactionType");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasData(
                TransactionTypeEnum.List.Select(e => new {
                    Code = e.Value,
                    e.Name
                })
            );
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
