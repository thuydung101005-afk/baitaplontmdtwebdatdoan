using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Data;

public partial class FoodieDbContext : DbContext
{
    public FoodieDbContext(DbContextOptions<FoodieDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<RestaurantFollower> RestaurantFollowers { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ItemOption> ItemOptions { get; set; }

    public virtual DbSet<ItemOptionGroup> ItemOptionGroups { get; set; }

    public virtual DbSet<MenuCategory> MenuCategories { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OauthAccount> OauthAccounts { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OtpCode> OtpCodes { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Restaurant> Restaurants { get; set; }

    public virtual DbSet<RestaurantOperatingHour> RestaurantOperatingHours { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Shipper> Shippers { get; set; }

    public virtual DbSet<ShipperLocationsLog> ShipperLocationsLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDevice> UserDevices { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherUsage> VoucherUsages { get; set; }

    public virtual DbSet<VwDailyRevenue> VwDailyRevenues { get; set; }

    public virtual DbSet<VwOrderStatusSummary> VwOrderStatusSummaries { get; set; }

    public virtual DbSet<VwRestaurantRevenue> VwRestaurantRevenues { get; set; }

    public virtual DbSet<VwShipperPerformance> VwShipperPerformances { get; set; }

    public virtual DbSet<VwTopSellingItem> VwTopSellingItems { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__addresse__3213E83F78629E28");

            entity.ToTable("addresses");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "idx_addresses_geo");

            entity.HasIndex(e => e.UserId, "idx_addresses_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddressLine)
                .HasMaxLength(255)
                .HasColumnName("address_line");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Label)
                .HasMaxLength(50)
                .HasColumnName("label");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("longitude");
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(150)
                .HasColumnName("receiver_name");
            entity.Property(e => e.ReceiverPhone)
                .HasMaxLength(20)
                .HasColumnName("receiver_phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Ward)
                .HasMaxLength(100)
                .HasColumnName("ward");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_address_user");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__audit_lo__3213E83F0DC08CB8");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_audit_entity");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .HasColumnName("entity_type");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("fk_audit_actor");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__cart_ite__3213E83FB77BADC8");

            entity.ToTable("cart_items");

            entity.HasIndex(e => e.UserId, "idx_cart_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.SelectedOptions).HasColumnName("selected_options");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cart_item");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cart_restaurant");

            entity.HasOne(d => d.User).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_cart_user");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RestaurantId }).HasName("pk_favorites");

            entity.ToTable("favorites");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_favorite_restaurant");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_favorite_user");
        });

        modelBuilder.Entity<ItemOption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__item_opt__3213E83FCA73600C");

            entity.ToTable("item_options");

            entity.HasIndex(e => e.GroupId, "idx_option_group");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExtraPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("extra_price");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.Group).WithMany(p => p.ItemOptions)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("fk_option_group");
        });

        modelBuilder.Entity<ItemOptionGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__item_opt__3213E83F9CACFD5B");

            entity.ToTable("item_option_groups");

            entity.HasIndex(e => e.MenuItemId, "idx_group_item");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsRequired).HasColumnName("is_required");
            entity.Property(e => e.MaxSelect)
                .HasDefaultValue(1)
                .HasColumnName("max_select");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");
            entity.Property(e => e.MinSelect).HasColumnName("min_select");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.ItemOptionGroups)
                .HasForeignKey(d => d.MenuItemId)
                .HasConstraintName("fk_group_item");
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__menu_cat__3213E83F2E12A889");

            entity.ToTable("menu_categories");

            entity.HasIndex(e => e.RestaurantId, "idx_category_restaurant");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.MenuCategories)
                .HasForeignKey(d => d.RestaurantId)
                .HasConstraintName("fk_category_restaurant");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__menu_ite__3213E83F6145BCEC");

            entity.ToTable("menu_items");

            entity.HasIndex(e => new { e.RestaurantId, e.IsAvailable }, "idx_items_restaurant_available");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.IsBestSeller).HasColumnName("is_best_seller");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_item_category");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.RestaurantId)
                .HasConstraintName("fk_item_restaurant");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__notifica__3213E83FE5FE9EB0");

            entity.ToTable("notifications");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "idx_notification_user_read");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_notification_user");
        });

        modelBuilder.Entity<OauthAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__oauth_ac__3213E83F29F90540");

            entity.ToTable("oauth_accounts");

            entity.HasIndex(e => new { e.Provider, e.ProviderUid }, "uq_provider_uid").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderUid)
                .HasMaxLength(255)
                .HasColumnName("provider_uid");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.OauthAccounts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_oauth_user");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__orders__3213E83F16BEBB5A");

            entity.ToTable("orders", tb => tb.HasTrigger("trg_orders_updated_at"));

            entity.HasIndex(e => e.OrderCode, "UQ__orders__99D12D3FAE8645A0").IsUnique();

            entity.HasIndex(e => new { e.RestaurantId, e.OrderStatus }, "idx_orders_restaurant_status");

            entity.HasIndex(e => e.ShipperId, "idx_orders_shipper");

            entity.HasIndex(e => new { e.UserId, e.OrderStatus }, "idx_orders_user_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(255)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryAddressId).HasColumnName("delivery_address_id");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(20)
                .HasColumnName("order_code");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("order_status");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("unpaid")
                .HasColumnName("payment_status");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.ShipperId).HasColumnName("shipper_id");
            entity.Property(e => e.ShippingFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_fee");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.DeliveryAddress).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DeliveryAddressId)
                .HasConstraintName("fk_order_address");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.Orders)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_restaurant");

            entity.HasOne(d => d.Shipper).WithMany(p => p.OrderShippers)
                .HasForeignKey(d => d.ShipperId)
                .HasConstraintName("fk_order_shipper");

            entity.HasOne(d => d.User).WithMany(p => p.OrderUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_user");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__order_it__3213E83F119DC2AE");

            entity.ToTable("order_items");

            entity.HasIndex(e => e.OrderId, "idx_orderitem_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemName)
                .HasMaxLength(200)
                .HasColumnName("item_name");
            entity.Property(e => e.LineTotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("line_total");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SelectedOptions).HasColumnName("selected_options");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orderitem_item");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_orderitem_order");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__order_st__3213E83F2A6B2124");

            entity.ToTable("order_status_history");

            entity.HasIndex(e => e.OrderId, "idx_history_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("changed_at");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_history_order");
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__otp_code__3213E83F26B2BAE2");

            entity.ToTable("otp_codes");

            entity.HasIndex(e => new { e.Target, e.Purpose }, "idx_otp_target_purpose");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Attempts).HasColumnName("attempts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.OtpHash)
                .HasMaxLength(255)
                .HasColumnName("otp_hash");
            entity.Property(e => e.Purpose)
                .HasMaxLength(20)
                .HasColumnName("purpose");
            entity.Property(e => e.Target)
                .HasMaxLength(150)
                .HasColumnName("target");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.OtpCodes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_otp_user");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__payments__3213E83F2DBDCA57");

            entity.ToTable("payments");

            entity.HasIndex(e => e.TransactionId, "UQ__payments__85C600AE1AC2C511").IsUnique();

            entity.HasIndex(e => e.OrderId, "idx_payment_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.RawResponse).HasColumnName("raw_response");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(150)
                .HasColumnName("transaction_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_payment_order");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__refresh___3213E83F45EBFC13");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.ExpiresAt, "idx_refreshtoken_expiry");

            entity.HasIndex(e => e.UserId, "idx_refreshtoken_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_refreshtoken_user");
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__restaura__3213E83FA9ED150C");

            entity.ToTable("restaurants", tb => tb.HasTrigger("trg_restaurants_updated_at"));

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "idx_restaurants_geo");

            entity.HasIndex(e => e.Status, "idx_restaurants_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AvgRating)
                .HasColumnType("decimal(2, 1)")
                .HasColumnName("avg_rating");
            entity.Property(e => e.CoverImageUrl)
                .HasMaxLength(500)
                .HasColumnName("cover_image_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("latitude");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .HasColumnName("logo_url");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("longitude");
            entity.Property(e => e.MinOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("min_order_amount");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PrepTimeMinutes)
                .HasDefaultValue(15)
                .HasColumnName("prep_time_minutes");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending_approval")
                .HasColumnName("status");
            entity.Property(e => e.TotalReviews).HasColumnName("total_reviews");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Restaurants)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_restaurant_owner");
        });

        modelBuilder.Entity<RestaurantOperatingHour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__restaura__3213E83F295B8EC8");

            entity.ToTable("restaurant_operating_hours");

            entity.HasIndex(e => e.RestaurantId, "idx_hours_restaurant");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CloseTime).HasColumnName("close_time");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.OpenTime).HasColumnName("open_time");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.RestaurantOperatingHours)
                .HasForeignKey(d => d.RestaurantId)
                .HasConstraintName("fk_hours_restaurant");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__reviews__3213E83F3E319521");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.RestaurantId, "idx_review_restaurant");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReplyFromOwner).HasColumnName("reply_from_owner");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_order");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_restaurant");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_user");
        });

        modelBuilder.Entity<Shipper>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__shippers__B9BE370F485894D8");

            entity.ToTable("shippers");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentLat)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("current_lat");
            entity.Property(e => e.CurrentLng)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("current_lng");
            entity.Property(e => e.IsOnline).HasColumnName("is_online");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .HasColumnName("license_plate");
            entity.Property(e => e.RatingAvg)
                .HasColumnType("decimal(2, 1)")
                .HasColumnName("rating_avg");
            entity.Property(e => e.VehicleType)
                .HasMaxLength(50)
                .HasColumnName("vehicle_type");

            entity.HasOne(d => d.User).WithOne(p => p.Shipper)
                .HasForeignKey<Shipper>(d => d.UserId)
                .HasConstraintName("fk_shipper_user");
        });

        modelBuilder.Entity<ShipperLocationsLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__shipper___3213E83F53E0F7E4");

            entity.ToTable("shipper_locations_log");

            entity.HasIndex(e => e.OrderId, "idx_loc_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Lat)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("lat");
            entity.Property(e => e.Lng)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("lng");
            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("logged_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ShipperId).HasColumnName("shipper_id");

            entity.HasOne(d => d.Order).WithMany(p => p.ShipperLocationsLogs)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_loc_order");

            entity.HasOne(d => d.Shipper).WithMany(p => p.ShipperLocationsLogs)
                .HasForeignKey(d => d.ShipperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_loc_shipper");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__users__3213E83FC843D707");

            entity.ToTable("users", tb => tb.HasTrigger("trg_users_updated_at"));

            entity.HasIndex(e => e.Email, "UQ__users__AB6E61647FEAA7F4").IsUnique();

            entity.HasIndex(e => e.Phone, "UQ__users__B43B145FBBF0AB06").IsUnique();

            entity.HasIndex(e => e.Role, "idx_users_role");

            entity.HasIndex(e => e.Status, "idx_users_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("customer")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__user_dev__3213E83F27D128DF");

            entity.ToTable("user_devices");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceToken)
                .HasMaxLength(255)
                .HasColumnName("device_token");
            entity.Property(e => e.Platform)
                .HasMaxLength(10)
                .HasColumnName("platform");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserDevices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_device_user");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__vouchers__3213E83F50803427");

            entity.ToTable("vouchers");

            entity.HasIndex(e => e.Code, "UQ__vouchers__357D4CF957B97D20").IsUnique();

            entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate }, "idx_voucher_active_dates");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxDiscount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_discount");
            entity.Property(e => e.MinOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("min_order_amount");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsageLimitPerUser)
                .HasDefaultValue(1)
                .HasColumnName("usage_limit_per_user");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.RestaurantId)
                .HasConstraintName("fk_voucher_restaurant");
        });

        modelBuilder.Entity<VoucherUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__voucher___3213E83F33729EDC");

            entity.ToTable("voucher_usages");

            entity.HasIndex(e => new { e.VoucherId, e.UserId }, "idx_usage_voucher_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Order).WithMany(p => p.VoucherUsages)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usage_order");

            entity.HasOne(d => d.User).WithMany(p => p.VoucherUsages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usage_user");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherUsages)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usage_voucher");
        });

        modelBuilder.Entity<VwDailyRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_daily_revenue");

            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.TotalDiscount)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_discount");
            entity.Property(e => e.TotalOrders).HasColumnName("total_orders");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_revenue");
            entity.Property(e => e.TotalShippingFee)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_shipping_fee");
        });

        modelBuilder.Entity<VwOrderStatusSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_order_status_summary");

            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasColumnName("order_status");
            entity.Property(e => e.Total).HasColumnName("total");
            entity.Property(e => e.TotalValue)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_value");
        });

        modelBuilder.Entity<VwRestaurantRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_restaurant_revenue");

            entity.Property(e => e.AvgRating)
                .HasColumnType("decimal(2, 1)")
                .HasColumnName("avg_rating");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.RestaurantName)
                .HasMaxLength(200)
                .HasColumnName("restaurant_name");
            entity.Property(e => e.TotalOrders).HasColumnName("total_orders");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_revenue");
        });

        modelBuilder.Entity<VwShipperPerformance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_shipper_performance");

            entity.Property(e => e.RatingAvg)
                .HasColumnType("decimal(2, 1)")
                .HasColumnName("rating_avg");
            entity.Property(e => e.ShipperId).HasColumnName("shipper_id");
            entity.Property(e => e.ShipperName)
                .HasMaxLength(150)
                .HasColumnName("shipper_name");
            entity.Property(e => e.TotalCancelled).HasColumnName("total_cancelled");
            entity.Property(e => e.TotalDelivered).HasColumnName("total_delivered");
        });

        modelBuilder.Entity<VwTopSellingItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_top_selling_items");

            entity.Property(e => e.ItemName)
                .HasMaxLength(200)
                .HasColumnName("item_name");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.RestaurantName)
                .HasMaxLength(200)
                .HasColumnName("restaurant_name");
            entity.Property(e => e.TotalQuantitySold).HasColumnName("total_quantity_sold");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_revenue");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__wallets__B9BE370FFF0954C1");

            entity.ToTable("wallets");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("balance");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.UserId)
                .HasConstraintName("fk_wallet_user");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__wallet_t__3213E83FE178CECA");

            entity.ToTable("wallet_transactions");

            entity.HasIndex(e => e.UserId, "idx_wallettx_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ReferenceOrderId).HasColumnName("reference_order_id");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReferenceOrder).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ReferenceOrderId)
                .HasConstraintName("fk_wallettx_order");

            entity.HasOne(d => d.User).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallettx_user");
        });

        modelBuilder.Entity<RestaurantFollower>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RestaurantId }).HasName("pk_restaurant_followers");
            entity.ToTable("restaurant_followers");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");

            entity.HasOne(d => d.Restaurant).WithMany()
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_rf_restaurant");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_rf_user");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("chat_messages");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.SenderRole).HasMaxLength(50).HasColumnName("sender_role");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");

            entity.HasOne(d => d.Sender).WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chat_sender");

            entity.HasOne(d => d.Restaurant).WithMany()
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_chat_restaurant");

            entity.HasOne(d => d.Customer).WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chat_customer");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
