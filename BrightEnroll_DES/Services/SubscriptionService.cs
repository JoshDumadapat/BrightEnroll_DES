using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<Subscription>> GetAllAsync();
    }

    /// <summary>
    /// Business logic for subscription metrics in System Admin portal.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;

        public SubscriptionService(ISubscriptionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<IEnumerable<Subscription>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }
    }
}


