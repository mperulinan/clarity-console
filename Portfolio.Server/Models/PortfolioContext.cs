using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Server.Models;

public partial class PortfolioContext : DbContext
{
    public PortfolioContext()
    {
    }

    public PortfolioContext(DbContextOptions<PortfolioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Trade> Trades { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Portfolio");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__YourTabl__3214EC0704F226F8");

            entity.ToTable("Trade");

            entity.Property(e => e.AmountReceived).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.AmountSpent).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Fee).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FeeAsset).HasMaxLength(50);
            entity.Property(e => e.FeeAssetPriceInEur).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.FromAssetId).HasMaxLength(50);
            entity.Property(e => e.FromAssetPriceInEur).HasColumnType("decimal(36, 18)");
            entity.Property(e => e.ToAssetId).HasMaxLength(50);
            entity.Property(e => e.TransactionType).HasMaxLength(50);

            entity.HasOne(d => d.TransactionTypeNavigation).WithMany(p => p.Trades)
                .HasForeignKey(d => d.TransactionType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trade_TransactionType");
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.Code);

            entity.ToTable("TransactionType");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
