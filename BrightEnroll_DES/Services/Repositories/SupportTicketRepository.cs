using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    public interface ISupportTicketRepository
    {
        Task<IEnumerable<SupportTicket>> GetAllAsync();
    }

    /// <summary>
    /// Repository for support tickets (tbl_SupportTickets).
    /// </summary>
    public class SupportTicketRepository : BaseRepository, ISupportTicketRepository
    {
        public SupportTicketRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<IEnumerable<SupportTicket>> GetAllAsync()
        {
            const string query = @"
                SELECT [ticket_id], [school_name], [subject], [description],
                       [priority], [status], [assigned_agent], [created_at]
                FROM [dbo].[tbl_SupportTickets]";

            var table = await ExecuteQueryAsync(query);
            var list = new List<SupportTicket>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new SupportTicket
                {
                    ticket_id = Convert.ToInt32(row["ticket_id"]),
                    school_name = row["school_name"].ToString() ?? string.Empty,
                    subject = row["subject"].ToString() ?? string.Empty,
                    description = row["description"] == DBNull.Value ? null : row["description"].ToString(),
                    priority = row["priority"].ToString() ?? "Medium",
                    status = row["status"].ToString() ?? "Open",
                    assigned_agent = row["assigned_agent"] == DBNull.Value ? null : row["assigned_agent"].ToString(),
                    created_at = row["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["created_at"])
                });
            }

            return list;
        }
    }
}


