using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data;

public class MainContext(DbContextOptions<MainContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        CreateRelations(builder);
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
       
    }

    private static void CreateRelations(ModelBuilder builder)
    {
        
    }
}