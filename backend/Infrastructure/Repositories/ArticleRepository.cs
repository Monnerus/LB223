using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ArticleRepository(AppDbContext db) : IArticleRepository
{
    public async Task<IEnumerable<Article>> GetAllAsync() =>
        await db.Articles.AsNoTracking().ToListAsync();

    public async Task<Article?> GetByIdAsync(int id) =>
        await db.Articles.FindAsync(id);

    public async Task BookAsync(int id, BookingType type, int amount)
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        
        var article = await db.Articles
            .FromSqlRaw("SELECT * FROM Articles WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Article {id} not found.");

        var delta = type == BookingType.Einbuchen ? amount : -amount;

        if (article.Quantity + delta < 0)
            throw new InvalidOperationException(
                $"Nicht genug Bestand. Verfügbar: {article.Quantity}");

        article.Quantity += delta;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
