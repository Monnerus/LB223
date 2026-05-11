using Core.Entities;

namespace Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> AnyAsync();
    Task AddAsync(User user);
}
