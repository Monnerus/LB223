using Core.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class LoadTests
{
    private const string ConnectionString =
        "Server=localhost,1433;Database=LB223;User Id=sa;Password=LB223_Dev!Pass;TrustServerCertificate=True;";
    private const int ArticleId = 2;

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    private async Task ResetQuantityAsync(int quantity)
    {
        await using var db = CreateContext();
        await db.Articles
            .Where(a => a.Id == ArticleId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Quantity, quantity));
    }
    
    [Fact]
    public async Task LastTest_Ausbuchen_BestandNieUnterNull()
    {
        const int startQuantity = 5;
        const int taskCount = 10;
        await ResetQuantityAsync(startQuantity);

        int successCount = 0;
        int failCount = 0;

        var tasks = Enumerable.Range(0, taskCount).Select(async _ =>
        {
            await using var db = CreateContext();
            var repo = new ArticleRepository(db);
            try
            {
                await repo.BookAsync(ArticleId, BookingType.Ausbuchen, 1);
                Interlocked.Increment(ref successCount);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref failCount);
            }
        });

        await Task.WhenAll(tasks);

        await using var verifyDb = CreateContext();
        var article = await verifyDb.Articles.FindAsync(ArticleId);

        Assert.True(article!.Quantity >= 0, "Bestand darf nie unter 0 fallen.");
        Assert.Equal(0, article.Quantity);
        Assert.Equal(startQuantity, successCount);
        Assert.Equal(taskCount - startQuantity, failCount);
    }

    [Fact]
    public async Task LastTest_Einbuchen_AlleErfolgreich()
    {
        const int taskCount = 20;
        await ResetQuantityAsync(0);

        var tasks = Enumerable.Range(0, taskCount).Select(async _ =>
        {
            await using var db = CreateContext();
            var repo = new ArticleRepository(db);
            await repo.BookAsync(ArticleId, BookingType.Einbuchen, 1);
        });

        await Task.WhenAll(tasks);

        await using var verifyDb = CreateContext();
        var article = await verifyDb.Articles.FindAsync(ArticleId);

        Assert.Equal(taskCount, article!.Quantity);
    }
}
