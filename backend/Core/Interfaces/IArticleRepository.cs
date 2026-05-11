using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IArticleRepository
{
    Task<IEnumerable<Article>> GetAllAsync();
    Task<Article?> GetByIdAsync(int id);
    Task BookAsync(int id, BookingType type, int amount);
}
