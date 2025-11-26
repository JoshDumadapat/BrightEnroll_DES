using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;

namespace BrightEnroll_DES.Services
{
    public interface ISupportTicketService
    {
        Task<IEnumerable<SupportTicket>> GetAllAsync();
    }

    /// <summary>
    /// Business logic for System Admin support tickets.
    /// </summary>
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ISupportTicketRepository _repository;

        public SupportTicketService(ISupportTicketRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Task<IEnumerable<SupportTicket>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }
    }
}


