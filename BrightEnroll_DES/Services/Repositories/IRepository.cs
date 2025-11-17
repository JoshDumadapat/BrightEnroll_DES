using System.Data;

namespace BrightEnroll_DES.Services.Repositories
{
    /// <summary>
    /// Base repository interface for ORM-like database operations
    /// Provides a consistent pattern for all data access operations
    /// </summary>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves a single entity by its primary key
        /// </summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Inserts a new entity
        /// </summary>
        Task<int> InsertAsync(T entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task<int> UpdateAsync(T entity);

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// Checks if an entity exists by its primary key
        /// </summary>
        Task<bool> ExistsAsync(int id);
    }
}

