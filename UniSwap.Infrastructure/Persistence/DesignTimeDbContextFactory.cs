using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UniSwap.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UniSwapDbContext>
{
    public UniSwapDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__UniSwap")
            ?? throw new InvalidOperationException("Set ConnectionStrings__UniSwap before running EF tools.");
        return new UniSwapDbContext(new DbContextOptionsBuilder<UniSwapDbContext>().UseNpgsql(connection).Options);
    }
}
