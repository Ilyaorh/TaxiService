using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using TaxiService.Data.Models;

namespace TaxiService.Data
{
    /// <summary>Создаёт контекст с настройкой подключения по умолчанию</summary>
    public class TaxiServiceContext : DbContext
    {
        public TaxiServiceContext() { }

        /// <summary>Создаёт контекст с явной передачей параметров EF Core</summary>
        public TaxiServiceContext(DbContextOptions<TaxiServiceContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Car> Cars => Set<Car>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Tariff> Tariffs => Set<Tariff>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
        public DbSet<PromoCodeUsage> PromoCodeUsages => Set<PromoCodeUsage>();
        public DbSet<DriverRating> DriverRatings => Set<DriverRating>();

        /// <summary>Автоматически подставляет строку подключения к SQL Server</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=172.16.1.101,33678;Database=TaxiServiceDB;User Id=Orehov;Password=Ai9B)5;MultipleActiveResultSets=true;TrustServerCertificate=True;");
            }
        }

        /// <summary>Настраивает точные типы decimal для числовых столбцов БД</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(e => e.TripID);
                entity.Property(e => e.DistanceKm).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(10,2)");
                entity.Property(e => e.ServiceCommission).HasColumnType("decimal(10,2)");
                entity.Property(e => e.OriginalCost).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<Driver>(entity =>
            {
                entity.Property(e => e.Rating).HasColumnType("decimal(3,2)");
            });

            modelBuilder.Entity<Tariff>(entity =>
            {
                entity.Property(e => e.BasePrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.PricePerKm).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CommissionPercent).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<PromoCode>(entity =>
            {
                entity.HasKey(e => e.PromoCodeID);
                entity.Property(e => e.DiscountValue).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<PromoCodeUsage>(entity =>
            {
                entity.HasKey(e => e.UsageID);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<DriverRating>(entity =>
            {
                entity.HasKey(e => e.DriverRatingID);
                entity.HasIndex(e => e.DriverID).IsUnique();
                entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)");
            });
        }
    }
}