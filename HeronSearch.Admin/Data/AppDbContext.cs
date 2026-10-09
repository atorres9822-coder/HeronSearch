using HeronSearch.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Data;

// The EF Core database context: the four HeronSearch tables and every column, index, and foreign key rule.
// Controllers receive it through dependency injection, and the InitialCreate migration is built from this model.
public class AppDbContext : DbContext
{
    // Creates the context with the options from Program.cs (the MySQL provider and the DefaultConnection string).
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // The Users table: Admin, Staff, and Student accounts.
    public DbSet<User> Users => Set<User>();

    // The Items table: lost and found item reports.
    public DbSet<Item> Items => Set<Item>();

    // The Claims table: ownership claims filed for items.
    public DbSet<Claim> Claims => Set<Claim>();

    // The ActivityLogs table: the history of sign-ins and admin actions.
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // Describes the complete schema: required and optional columns, maximum lengths, enums stored as text,
    // indexes, and foreign keys with their delete rules. Demo data is not defined here; DbSeeder adds it at run time.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users: Email is unique, and Role and VerificationStatus are stored as text such as "Verified".
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(150);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            e.Property(u => u.StudentId).IsRequired(false).HasMaxLength(20);
            e.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.VerificationStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.CreatedAt).IsRequired();
        });

        // Items: Type and Status are stored as text, and DateReported has an index for date filters and charts.
        modelBuilder.Entity<Item>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).IsRequired().HasMaxLength(100);
            e.Property(i => i.Category).IsRequired().HasMaxLength(50);
            e.Property(i => i.Location).IsRequired().HasMaxLength(100);
            e.Property(i => i.DateReported).IsRequired();
            e.HasIndex(i => i.DateReported);
            e.Property(i => i.Description).IsRequired(false).HasMaxLength(1000);
            e.Property(i => i.ImagePath).IsRequired(false).HasMaxLength(260);
            e.Property(i => i.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

            // The user who reported the item (User.ReportedItems). Restrict: a user who still has items cannot be deleted.
            e.HasOne(i => i.ReportedByUser)
                .WithMany(u => u.ReportedItems)
                .HasForeignKey(i => i.ReportedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Claims: Status is stored as text and is a concurrency token, so when two people review the same claim
        // at once, the second save fails instead of overwriting the first review.
        modelBuilder.Entity<Claim>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ProofDescription).IsRequired().HasMaxLength(1000);
            e.Property(c => c.DateFiled).IsRequired();
            e.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
            e.Property(c => c.RejectionReason).IsRequired(false).HasMaxLength(500);
            e.Property(c => c.ReviewedAt).IsRequired(false);

            // The claimed item (Item.Claims). Restrict: an item that still has claims cannot be deleted.
            e.HasOne(c => c.Item)
                .WithMany(i => i.Claims)
                .HasForeignKey(c => c.ItemId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // The user who filed the claim (User.FiledClaims). Restrict: a user who still has claims cannot be deleted.
            e.HasOne(c => c.ClaimantUser)
                .WithMany(u => u.FiledClaims)
                .HasForeignKey(c => c.ClaimantUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // The reviewer (User.ReviewedClaims); optional because it stays empty while the claim is Pending.
            // Restrict: a user who reviewed claims cannot be deleted.
            e.HasOne(c => c.ReviewedByUser)
                .WithMany(u => u.ReviewedClaims)
                .HasForeignKey(c => c.ReviewedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ActivityLogs: Timestamp has an index for the newest-first lists. Deleting a user or an item keeps
        // its log rows and sets the link to null (SetNull), so the history is never lost.
        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Timestamp).IsRequired();
            e.HasIndex(a => a.Timestamp);
            e.Property(a => a.Action).IsRequired().HasMaxLength(50);
            e.Property(a => a.Result).IsRequired().HasMaxLength(500);

            // The user who acted; optional, and User has no list of logs.
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // The item the action was about; optional, and Item has no list of logs.
            e.HasOne(a => a.TargetItem)
                .WithMany()
                .HasForeignKey(a => a.TargetItemId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
