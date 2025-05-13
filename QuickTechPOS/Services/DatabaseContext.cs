// File: QuickTechPOS/Services/DatabaseContext.cs

using Microsoft.EntityFrameworkCore;
using QuickTechPOS.Models;
using QuickTechPOS.Models.Enums;
using System;

namespace QuickTechPOS.Services
{
    /// <summary>
    /// Provides database access and operations for the POS system
    /// </summary>
    public class DatabaseContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Employees table in the database
        /// </summary>
        public DbSet<Employee> Employees { get; set; }

        /// <summary>
        /// Drawers table in the database
        /// </summary>
        public DbSet<Drawer> Drawers { get; set; }

        /// <summary>
        /// Products table in the database
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// Transactions table in the database
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Failed transactions table in the database
        /// </summary>
        public DbSet<FailedTransaction> FailedTransactions { get; set; }

        /// <summary>
        /// Transaction details table in the database
        /// </summary>
        public DbSet<TransactionDetail> TransactionDetails { get; set; }

        /// <summary>
        /// Customers table in the database
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// Customer product prices table in the database
        /// </summary>
        public DbSet<CustomerProductPrice> CustomerProductPrices { get; set; }

        /// <summary>
        /// Drawer transactions table in the database
        /// </summary>
        public DbSet<DrawerTransaction> DrawerTransactions { get; set; }

        /// <summary>
        /// Drawer history entries table in the database
        /// </summary>
        public DbSet<DrawerHistoryEntry> DrawerHistoryEntries { get; set; }

        /// <summary>
        /// Business settings table in the database
        /// </summary>
        public DbSet<BusinessSetting> BusinessSettings { get; set; }

        /// <summary>
        /// Initializes a new instance of the database context
        /// </summary>
        /// <param name="connectionString">Connection string to the database</param>
        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Configures the database connection
        /// </summary>
        /// <param name="optionsBuilder">Options builder for database configuration</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);

                // Enable detailed SQL logging for troubleshooting
                DatabaseDiagnostics.EnableSqlLogging(optionsBuilder);
            }
        }

        /// <summary>
        /// Configures the entity models
        /// </summary>
        /// <param name="modelBuilder">Model builder for entity configuration</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Employee entity
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.MonthlySalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CurrentBalance).HasColumnType("decimal(18,2)");
            });

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.Barcode).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CurrentStock).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Speed).HasMaxLength(50);
                entity.Property(e => e.ImagePath).HasMaxLength(500);
                entity.Property(e => e.BoxBarcode).HasMaxLength(50);
                entity.Property(e => e.BoxPurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BoxSalePrice).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");

                // Convert TransactionStatus enum to string in the database
                entity.Property(e => e.Status)
              .HasConversion<string>()
              .HasMaxLength(50);

                // Convert TransactionType enum to string in the database
                entity.Property(e => e.TransactionType)
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.CashierId).HasMaxLength(50);
                entity.Property(e => e.CashierName).HasMaxLength(100);
                entity.Property(e => e.CashierRole).HasMaxLength(50);
            });

            // Configure TransactionDetail entity
            modelBuilder.Entity<TransactionDetail>(entity =>
            {
                entity.HasKey(e => e.TransactionDetailId);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Transaction)
                    .WithMany()
                    .HasForeignKey(d => d.TransactionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure FailedTransaction entity
            modelBuilder.Entity<FailedTransaction>(entity =>
            {
                entity.HasKey(ft => ft.FailedTransactionId);
                entity.Property(ft => ft.ErrorMessage).HasMaxLength(1000);
                entity.Property(ft => ft.ErrorDetails).HasMaxLength(4000);
                entity.Property(ft => ft.FailureComponent).HasMaxLength(100);
                entity.Property(ft => ft.CashierName).HasMaxLength(100);
                entity.Property(ft => ft.CustomerName).HasMaxLength(200);
                entity.Property(ft => ft.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(ft => ft.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(ft => ft.PaymentMethod).HasMaxLength(50);

                // Convert FailedTransactionState enum to string in the database
                entity.Property(ft => ft.State)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Convert TransactionType enum to string in the database
                entity.Property(ft => ft.TransactionType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasOne(ft => ft.OriginalTransaction)
                    .WithMany()
                    .HasForeignKey(ft => ft.OriginalTransactionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Phone).HasMaxLength(20);
                entity.Property(c => c.Email).HasMaxLength(100);
                entity.Property(c => c.Address).HasMaxLength(500);
                entity.Property(c => c.Balance).HasColumnType("decimal(18,2)");
            });

            // Configure CustomerProductPrice entity
            modelBuilder.Entity<CustomerProductPrice>(entity =>
            {
                entity.HasKey(e => e.CustomerProductPriceId);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                // Create composite index for faster lookups
                entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();

                // Setup relationships
                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                          .WithMany()
                          .HasForeignKey(d => d.ProductId)
                          .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Drawer entity
            modelBuilder.Entity<Drawer>(entity =>
            {
                entity.HasKey(d => d.DrawerId);
                entity.Property(d => d.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.Property(d => d.CurrentBalance).HasColumnType("decimal(18,2)");
                entity.Property(d => d.CashIn).HasColumnType("decimal(18,2)");
                entity.Property(d => d.CashOut).HasColumnType("decimal(18,2)");
                entity.Property(d => d.TotalSales).HasColumnType("decimal(18,2)");
                entity.Property(d => d.TotalExpenses).HasColumnType("decimal(18,2)");
                entity.Property(d => d.TotalSupplierPayments).HasColumnType("decimal(18,2)");
                entity.Property(d => d.NetCashFlow).HasColumnType("decimal(18,2)");
                entity.Property(d => d.DailySales).HasColumnType("decimal(18,2)");
                entity.Property(d => d.DailyExpenses).HasColumnType("decimal(18,2)");
                entity.Property(d => d.DailySupplierPayments).HasColumnType("decimal(18,2)");
                entity.Property(d => d.NetSales).HasColumnType("decimal(18,2)");
                entity.Property(d => d.Status).HasMaxLength(50);
                entity.Property(d => d.Notes).HasMaxLength(500).IsRequired(false); // Allow null values
                entity.Property(d => d.CashierId).HasMaxLength(50);
                entity.Property(d => d.CashierName).HasMaxLength(100);
            });

            // Configure DrawerTransaction entity
            modelBuilder.Entity<DrawerTransaction>(entity =>
            {
                entity.HasKey(dt => dt.TransactionId);
                entity.Property(dt => dt.Type).HasMaxLength(50);
                entity.Property(dt => dt.Amount).HasColumnType("decimal(18,2)");
                entity.Property(dt => dt.Balance).HasColumnType("decimal(18,2)");
                entity.Property(dt => dt.Notes).HasMaxLength(500);
                entity.Property(dt => dt.ActionType).HasMaxLength(50);
                entity.Property(dt => dt.Description).HasMaxLength(500);
                entity.Property(dt => dt.TransactionReference).HasMaxLength(50);
                entity.Property(dt => dt.PaymentMethod).HasMaxLength(50);

                entity.HasOne(dt => dt.Drawer)
                      .WithMany(d => d.Transactions)
                      .HasForeignKey(dt => dt.DrawerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DrawerHistoryEntry entity
            modelBuilder.Entity<DrawerHistoryEntry>(entity =>
            {
                entity.HasKey(dhe => dhe.Id);
                entity.Property(dhe => dhe.ActionType).HasMaxLength(50);
                entity.Property(dhe => dhe.Description).HasMaxLength(500);
                entity.Property(dhe => dhe.Amount).HasColumnType("decimal(18,2)");
                entity.Property(dhe => dhe.ResultingBalance).HasColumnType("decimal(18,2)");
                entity.Property(dhe => dhe.UserId).HasMaxLength(50);
            });

            // Configure BusinessSetting entity
            modelBuilder.Entity<BusinessSetting>(entity =>
            {
                entity.HasKey(bs => bs.Id);
                entity.Property(bs => bs.Key).IsRequired().HasMaxLength(100);
                entity.Property(bs => bs.Value).HasMaxLength(500);
                entity.Property(bs => bs.Description).HasMaxLength(500);
                entity.Property(bs => bs.Group).HasMaxLength(100);
                entity.Property(bs => bs.DataType).HasMaxLength(50);
                entity.Property(bs => bs.ModifiedBy).HasMaxLength(100);

                // Create index on Key column for faster lookups
                entity.HasIndex(bs => bs.Key).IsUnique();
            });
        }
    }
}