using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data;

public class MainContext : DbContext
{
    public MainContext(DbContextOptions<MainContext> options) : base(options)
    {
    }

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