using System.Data;

namespace BrightEnroll_DES.Services.Repositories
{
    // Base interface for repository pattern - standard CRUD operations
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

