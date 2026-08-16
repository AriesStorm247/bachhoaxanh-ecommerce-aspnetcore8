using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<CustomerVoucher> CustomerVouchers { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ProductInventory> ProductInventories { get; set; }
        public DbSet<InventoryBatch> InventoryBatches { get; set; }
        public DbSet<InventoryBatchDeduction> InventoryBatchDeductions { get; set; }
        public DbSet<ComboPromotion> ComboPromotions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotificationState> UserNotificationStates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CustomerProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<CustomerProfile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<CustomerProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductInventory>()
                .HasIndex(i => new { i.BranchId, i.ProductId });

            builder.Entity<ProductInventory>()
                .HasIndex(i => i.ProductId);

            builder.Entity<InventoryBatch>()
                .HasIndex(b => new { b.ProductId, b.BranchId, b.ExpiryDate });

            builder.Entity<InventoryBatch>()
                .HasOne(b => b.Product)
                .WithMany()
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InventoryBatch>()
                .HasOne(b => b.Branch)
                .WithMany()
                .HasForeignKey(b => b.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InventoryBatchDeduction>()
                .HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryBatchDeduction>()
                .HasOne(d => d.OrderDetail)
                .WithMany()
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryBatchDeduction>()
                .HasOne(d => d.InventoryBatch)
                .WithMany()
                .HasForeignKey(d => d.InventoryBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryBatchDeduction>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryBatchDeduction>()
                .HasOne(d => d.Branch)
                .WithMany()
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComboPromotion>()
                .HasOne(cp => cp.Product1)
                .WithMany()
                .HasForeignKey(cp => cp.ProductId1)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ComboPromotion>()
                .HasOne(cp => cp.Product2)
                .WithMany()
                .HasForeignKey(cp => cp.ProductId2)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserNotificationState>()
                .HasOne(ns => ns.User)
                .WithMany()
                .HasForeignKey(ns => ns.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserNotificationState>()
                .HasOne(ns => ns.Notification)
                .WithMany()
                .HasForeignKey(ns => ns.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>()
                .HaveColumnType("decimal(18, 2)");
        }
    }
}
