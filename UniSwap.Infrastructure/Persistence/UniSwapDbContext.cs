using Npgsql;
using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Interfaces;
using UniSwap.Application.Common;
using UniSwap.Application.Users;
using UniSwap.Domain.Entities;
using UniSwap.Domain.ValueObjects;

namespace UniSwap.Infrastructure.Persistence;

public sealed class UniSwapDbContext(DbContextOptions<UniSwapDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ExchangeRequest> ExchangeRequests => Set<ExchangeRequest>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var users = model.Entity<User>();
        users.HasKey(x => x.Id);
        users.Property(x => x.Name).HasMaxLength(200).IsRequired();
        users.Property(x => x.Email).HasConversion(x => x.Value, x => new Email(x)).HasMaxLength(320).IsRequired();
        users.Property<string>("NormalizedEmail")
            .HasComputedColumnSql("lower(btrim(\"Email\"))", stored: true)
            .HasMaxLength(320);
        users.HasIndex("NormalizedEmail").IsUnique();
        users.Property(x => x.PasswordHash).HasMaxLength(512);
        users.Property(x => x.Role).HasConversion<string>().HasMaxLength(16).HasDefaultValue(UserRole.User);

        var sessions = model.Entity<AccountSession>();
        sessions.HasKey(x => x.TokenHash);
        sessions.Property(x => x.TokenHash).HasMaxLength(64);
        sessions.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        var listings = model.Entity<Listing>();
        listings.Ignore(x => x.DomainEvents);
        listings.HasKey(x => x.Id);
        listings.Property<Guid>("ImagesRevision");
        listings.HasQueryFilter(x => x.DeletedAtUtc == null);
        listings.Property(x => x.Title).HasMaxLength(100).IsRequired();
        listings.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        listings.Property(x => x.Category).IsRequired();
        listings.Property(x => x.Condition).IsRequired();
        listings.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        listings.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");
        listings.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        listings.HasIndex(x => new { x.Status, x.PublishedAtUtc });

        var images = model.Entity<ListingImage>();
        images.HasKey(x => x.Id);
        images.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        images.HasIndex(x => x.StorageKey).IsUnique();
        images.Property(x => x.ContentType).HasMaxLength(32).IsRequired();
        images.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);

        var reservations = model.Entity<Reservation>();
        reservations.Ignore(x => x.DomainEvents);
        reservations.HasKey(x => x.Id);
        reservations.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        reservations.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");
        reservations.HasIndex(x => new { x.ListingId, x.RequesterId })
            .IsUnique().HasFilter("\"Status\" = 'Pending'").HasDatabaseName("UX_Reservations_PendingRequester");
        reservations.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
        reservations.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        reservations.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);

        var exchanges = model.Entity<ExchangeRequest>();
        exchanges.Ignore(x => x.DomainEvents);
        exchanges.HasKey(x => x.Id);
        exchanges.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        exchanges.Property<uint>("Version").IsRowVersion().HasColumnName("xmin");
        exchanges.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
        exchanges.HasOne<Listing>().WithMany().HasForeignKey(x => x.OfferedListingId).OnDelete(DeleteBehavior.Restrict);
        exchanges.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        exchanges.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new RequestConflictException(ConflictCodes.ConcurrentUpdate); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_Reservations_PendingRequester" })
        {
            throw new RequestConflictException(ConflictCodes.ReservationAlreadyPending);
        }
    }
}
