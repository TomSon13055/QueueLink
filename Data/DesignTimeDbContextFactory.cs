using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QueueLink.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connString =
            "Host=localhost;Port=5432;Database=QueueLinkDb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
