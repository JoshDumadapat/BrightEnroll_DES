using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    public interface IContractRepository
    {
        Task<IEnumerable<Contract>> GetAllAsync();
        Task<int> InsertAsync(Contract contract);
    }

    /// <summary>
    /// Repository for contracts (tbl_Contracts) using the shared DBConnection/BaseRepository.
    /// </summary>
    public class ContractRepository : BaseRepository, IContractRepository
    {
        public ContractRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<IEnumerable<Contract>> GetAllAsync()
        {
            const string query = @"
                SELECT [contract_id], [school_name], [customer_code],
                       [start_date], [end_date], [max_users],
                       [modules_admission], [modules_finance], [modules_hr],
                       [modules_grades], [modules_enrollment],
                       [status], [contract_file_path], [created_at]
                FROM [dbo].[tbl_Contracts]
                ORDER BY [end_date] DESC";

            var table = await ExecuteQueryAsync(query);
            var list = new List<Contract>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new Contract
                {
                    contract_id = Convert.ToInt32(row["contract_id"]),
                    school_name = row["school_name"].ToString() ?? string.Empty,
                    customer_code = row["customer_code"] == DBNull.Value ? null : row["customer_code"].ToString(),
                    start_date = Convert.ToDateTime(row["start_date"]),
                    end_date = Convert.ToDateTime(row["end_date"]),
                    max_users = row["max_users"] == DBNull.Value ? 0 : Convert.ToInt32(row["max_users"]),
                    modules_admission = row["modules_admission"] != DBNull.Value && Convert.ToBoolean(row["modules_admission"]),
                    modules_finance = row["modules_finance"] != DBNull.Value && Convert.ToBoolean(row["modules_finance"]),
                    modules_hr = row["modules_hr"] != DBNull.Value && Convert.ToBoolean(row["modules_hr"]),
                    modules_grades = row["modules_grades"] != DBNull.Value && Convert.ToBoolean(row["modules_grades"]),
                    modules_enrollment = row["modules_enrollment"] != DBNull.Value && Convert.ToBoolean(row["modules_enrollment"]),
                    status = row["status"].ToString() ?? "Active",
                    contract_file_path = row["contract_file_path"] == DBNull.Value ? null : row["contract_file_path"].ToString(),
                    created_at = row["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["created_at"])
                });
            }

            return list;
        }

        public async Task<int> InsertAsync(Contract contract)
        {
            const string query = @"
                INSERT INTO [dbo].[tbl_Contracts]
                    ([school_name], [customer_code], [start_date], [end_date], [max_users],
                     [modules_admission], [modules_finance], [modules_hr],
                     [modules_grades], [modules_enrollment],
                     [status], [contract_file_path])
                VALUES
                    (@SchoolName, @CustomerCode, @StartDate, @EndDate, @MaxUsers,
                     @ModulesAdmission, @ModulesFinance, @ModulesHr,
                     @ModulesGrades, @ModulesEnrollment,
                     @Status, @ContractFilePath)";

            var parameters = new[]
            {
                CreateParameter("@SchoolName", SanitizeString(contract.school_name, 150), SqlDbType.VarChar),
                CreateParameter("@CustomerCode", string.IsNullOrWhiteSpace(contract.customer_code) ? DBNull.Value : SanitizeString(contract.customer_code, 20), SqlDbType.VarChar),
                CreateParameter("@StartDate", contract.start_date, SqlDbType.Date),
                CreateParameter("@EndDate", contract.end_date, SqlDbType.Date),
                CreateParameter("@MaxUsers", contract.max_users, SqlDbType.Int),
                CreateParameter("@ModulesAdmission", contract.modules_admission, SqlDbType.Bit),
                CreateParameter("@ModulesFinance", contract.modules_finance, SqlDbType.Bit),
                CreateParameter("@ModulesHr", contract.modules_hr, SqlDbType.Bit),
                CreateParameter("@ModulesGrades", contract.modules_grades, SqlDbType.Bit),
                CreateParameter("@ModulesEnrollment", contract.modules_enrollment, SqlDbType.Bit),
                CreateParameter("@Status", SanitizeString(contract.status, 20), SqlDbType.VarChar),
                CreateParameter("@ContractFilePath", string.IsNullOrWhiteSpace(contract.contract_file_path) ? DBNull.Value : contract.contract_file_path, SqlDbType.VarChar)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }
    }
}


