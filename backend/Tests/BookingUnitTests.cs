using Core.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class BookingUnitTests
{
    private const string ConnectionString =
        "Server=localhost,1433;Database=LB223;User Id=sa;Password=LB223_Dev!Pass;TrustServerCertificate=True;";
    private const int ArticleId = 1;

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
    public async Task ParallelAusbuchen_NurEineErfolgreich()
    {
        await ResetQuantityAsync(1);

        int successCount = 0;
        int failCount = 0;

        var tasks = Enumerable.Range(0, 2).Select(async _ =>
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

        Assert.Equal(1, successCount);
        Assert.Equal(1, failCount);
        Assert.Equal(0, article!.Quantity);
    }
    
    [Fact]
    public async Task ParallelEinbuchen_AlleErfolgreich()
    {
        await ResetQuantityAsync(0);
        const int taskCount = 5;

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
