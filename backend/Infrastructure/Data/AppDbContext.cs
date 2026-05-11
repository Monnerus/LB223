using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(e =>
        {
            e.Property(a => a.RowVersion)
             .IsRowVersion();

            e.HasData(
                new Article { Id = 1, Name = "Maus Y2",     Quantity = 10 },
                new Article { Id = 2, Name = "Tastatur K1", Quantity = 5  },
                new Article { Id = 3, Name = "Monitor Z3",  Quantity = 3  }
            );
        });
    }
}
