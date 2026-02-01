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
                .HasMaxLength(50)
                .HasConversion(
                    v => v.Value, // To DB: "REWARD"
                    v => TransactionTypeEnum.FromValue(v) // From DB: TransactionTypeEnum.Reward
                );

            entity.HasOne(d => d.TransactionType).WithMany() // Assuming TransactionType doesn't need a collection of Transactions back for now, or use .WithMany("Transactions") if it exists
                .HasForeignKey(d => d.TransactionTypeCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transaction_TransactionType");
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
