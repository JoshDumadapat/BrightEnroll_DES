using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<int> InsertAsync(Customer customer);
    }

    /// <summary>
    /// Repository for System Admin customers (tbl_Customers).
    /// </summary>
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        public CustomerRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            const string query = @"
                SELECT [customer_id], [customer_code], [school_name], [school_type],
                       [address], [city], [province],
                       [contact_person], [contact_position], [contact_email], [contact_phone],
                       [plan], [monthly_fee],
                       [contract_start_date], [contract_duration_months], [contract_end_date],
                       [student_count], [status], [notes], [created_at]
                FROM [dbo].[tbl_Customers]
                ORDER BY [school_name]";

            var table = await ExecuteQueryAsync(query);
            var list = new List<Customer>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new Customer
                {
                    customer_id = Convert.ToInt32(row["customer_id"]),
                    customer_code = row["customer_code"].ToString() ?? string.Empty,
                    school_name = row["school_name"].ToString() ?? string.Empty,
                    school_type = row["school_type"] == DBNull.Value ? null : row["school_type"].ToString(),
                    address = row["address"].ToString() ?? string.Empty,
                    city = row["city"].ToString() ?? string.Empty,
                    province = row["province"] == DBNull.Value ? null : row["province"].ToString(),
                    contact_person = row["contact_person"].ToString() ?? string.Empty,
                    contact_position = row["contact_position"] == DBNull.Value ? null : row["contact_position"].ToString(),
                    contact_email = row["contact_email"].ToString() ?? string.Empty,
                    contact_phone = row["contact_phone"].ToString() ?? string.Empty,
                    plan = row["plan"].ToString() ?? string.Empty,
                    monthly_fee = row["monthly_fee"] == DBNull.Value ? 0 : Convert.ToDecimal(row["monthly_fee"]),
                    contract_start_date = Convert.ToDateTime(row["contract_start_date"]),
                    contract_duration_months = Convert.ToInt32(row["contract_duration_months"]),
                    contract_end_date = Convert.ToDateTime(row["contract_end_date"]),
                    student_count = row["student_count"] == DBNull.Value ? 0 : Convert.ToInt32(row["student_count"]),
                    status = row["status"].ToString() ?? "Active",
                    notes = row["notes"] == DBNull.Value ? null : row["notes"].ToString(),
                    created_at = row["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["created_at"])
                });
            }

            return list;
        }

        public async Task<int> InsertAsync(Customer customer)
        {
            const string query = @"
                INSERT INTO [dbo].[tbl_Customers]
                    ([customer_code], [school_name], [school_type],
                     [address], [city], [province],
                     [contact_person], [contact_position], [contact_email], [contact_phone],
                     [plan], [monthly_fee],
                     [contract_start_date], [contract_duration_months], [contract_end_date],
                     [student_count], [status], [notes])
                VALUES
                    (@CustomerCode, @SchoolName, @SchoolType,
                     @Address, @City, @Province,
                     @ContactPerson, @ContactPosition, @ContactEmail, @ContactPhone,
                     @Plan, @MonthlyFee,
                     @ContractStartDate, @ContractDurationMonths, @ContractEndDate,
                     @StudentCount, @Status, @Notes)";

            var parameters = new[]
            {
                CreateParameter("@CustomerCode", SanitizeString(customer.customer_code, 20), SqlDbType.VarChar),
                CreateParameter("@SchoolName", SanitizeString(customer.school_name, 150), SqlDbType.VarChar),
                CreateParameter("@SchoolType", string.IsNullOrWhiteSpace(customer.school_type) ? DBNull.Value : SanitizeString(customer.school_type, 50), SqlDbType.VarChar),
                CreateParameter("@Address", SanitizeString(customer.address, 255), SqlDbType.VarChar),
                CreateParameter("@City", SanitizeString(customer.city, 100), SqlDbType.VarChar),
                CreateParameter("@Province", string.IsNullOrWhiteSpace(customer.province) ? DBNull.Value : SanitizeString(customer.province, 100), SqlDbType.VarChar),
                CreateParameter("@ContactPerson", SanitizeString(customer.contact_person, 150), SqlDbType.VarChar),
                CreateParameter("@ContactPosition", string.IsNullOrWhiteSpace(customer.contact_position) ? DBNull.Value : SanitizeString(customer.contact_position, 100), SqlDbType.VarChar),
                CreateParameter("@ContactEmail", SanitizeString(customer.contact_email, 150), SqlDbType.VarChar),
                CreateParameter("@ContactPhone", SanitizeString(customer.contact_phone, 50), SqlDbType.VarChar),
                CreateParameter("@Plan", SanitizeString(customer.plan, 20), SqlDbType.VarChar),
                CreateParameter("@MonthlyFee", customer.monthly_fee, SqlDbType.Decimal),
                CreateParameter("@ContractStartDate", customer.contract_start_date, SqlDbType.Date),
                CreateParameter("@ContractDurationMonths", customer.contract_duration_months, SqlDbType.Int),
                CreateParameter("@ContractEndDate", customer.contract_end_date, SqlDbType.Date),
                CreateParameter("@StudentCount", customer.student_count, SqlDbType.Int),
                CreateParameter("@Status", SanitizeString(customer.status, 20), SqlDbType.VarChar),
                CreateParameter("@Notes", string.IsNullOrWhiteSpace(customer.notes) ? DBNull.Value : customer.notes, SqlDbType.Text)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }
    }
}


