using Microsoft.EntityFrameworkCore;
using UniSwap.Domain.Entities;
using UniSwap.Domain.Enums;
using UniSwap.Domain.ValueObjects;
using UniSwap.Infrastructure.Persistence;

internal static class PostgresSmokeTests
{
    public static async Task RunAsync(string connection)
    {
        var options = new DbContextOptionsBuilder<UniSwapDbContext>().UseNpgsql(connection).Options;
        await using var setup = new UniSwapDbContext(options);
        await setup.Database.MigrateAsync();

        var suffix = Guid.NewGuid().ToString("N");
        var owner = new User("Owner", new Email($"owner-{suffix}@example.test"));
        var requester = new User("Requester", new Email($"requester-{suffix}@example.test"));
        var competitor = new User("Competitor", new Email($"competitor-{suffix}@example.test"));
        var target = PublishedItem(owner.Id);
        var offered = PublishedItem(competitor.Id);

        var winner = new Reservation(target, requester.Id);
        var loser = new Reservation(target, competitor.Id);
        var exchange = new ExchangeRequest(target, offered, competitor.Id);
        setup.AddRange(owner, requester, competitor, target, offered, winner, loser, exchange);
        await setup.SaveChangesAsync();

        await using var reservationA = new UniSwapDbContext(options);
        await using var reservationB = new UniSwapDbContext(options);
        await using var exchangeScope = new UniSwapDbContext(options);

        var firstTarget = await reservationA.Listings.SingleAsync(x => x.Id == target.Id);
        var secondTarget = await reservationB.Listings.SingleAsync(x => x.Id == target.Id);
        var exchangeTarget = await exchangeScope.Listings.SingleAsync(x => x.Id == target.Id);
        var exchangeOffered = await exchangeScope.Listings.SingleAsync(x => x.Id == offered.Id);
        (await reservationA.Reservations.SingleAsync(x => x.Id == winner.Id)).Accept(owner.Id, firstTarget);
        (await reservationB.Reservations.SingleAsync(x => x.Id == loser.Id)).Accept(owner.Id, secondTarget);
        (await exchangeScope.ExchangeRequests.SingleAsync(x => x.Id == exchange.Id))
            .Accept(owner.Id, exchangeTarget, exchangeOffered);

        await reservationA.SaveChangesAsync();
        await ExpectConflictAsync(reservationB);
        await ExpectConflictAsync(exchangeScope);

        await using var check = new UniSwapDbContext(options);
        var requests = await check.Reservations.Where(x => x.ListingId == target.Id).ToDictionaryAsync(x => x.Id);
        var persistedExchange = await check.ExchangeRequests.SingleAsync(x => x.Id == exchange.Id);
        var listings = await check.Listings.Where(x => x.Id == target.Id || x.Id == offered.Id).ToDictionaryAsync(x => x.Id);
        if (requests[winner.Id].Status != ReservationStatus.Accepted ||
            requests[loser.Id].Status != ReservationStatus.Pending ||
            persistedExchange.Status != ExchangeStatus.Pending ||
            listings[target.Id].Status != ListingStatus.Reserved ||
            listings[offered.Id].Status != ListingStatus.Published)
            throw new InvalidOperationException("A stale write was committed or left partial changes.");

        Console.WriteLine("PostgreSQL migrations, competing acceptances and transaction rollback passed.");
    }

    private static Listing PublishedItem(Guid owner)
    {
        var listing = new Listing(owner, "A book", "A useful book", "Books", "Used");
        listing.Publish();
        return listing;
    }

    private static async Task ExpectConflictAsync(UniSwapDbContext context)
    {
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return; }
        throw new InvalidOperationException("A stale write succeeded.");
    }
}
