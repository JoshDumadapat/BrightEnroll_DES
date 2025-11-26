using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<IEnumerable<Subscription>> GetAllAsync();
    }

    /// <summary>
    /// Repository for subscription billing records (tbl_Subscriptions).
    /// </summary>
    public class SubscriptionRepository : BaseRepository, ISubscriptionRepository
    {
        public SubscriptionRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<IEnumerable<Subscription>> GetAllAsync()
        {
            const string query = @"
                SELECT [subscription_id], [customer_id], [plan], [monthly_fee],
                       [start_date], [end_date], [status], [created_at]
                FROM [dbo].[tbl_Subscriptions]";

            var table = await ExecuteQueryAsync(query);
            var list = new List<Subscription>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new Subscription
                {
                    subscription_id = Convert.ToInt32(row["subscription_id"]),
                    customer_id = Convert.ToInt32(row["customer_id"]),
                    plan = row["plan"].ToString() ?? string.Empty,
                    monthly_fee = row["monthly_fee"] == DBNull.Value ? 0 : Convert.ToDecimal(row["monthly_fee"]),
                    start_date = Convert.ToDateTime(row["start_date"]),
                    end_date = Convert.ToDateTime(row["end_date"]),
                    status = row["status"].ToString() ?? "Active",
                    created_at = row["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["created_at"])
                });
            }

            return list;
        }
    }
}


