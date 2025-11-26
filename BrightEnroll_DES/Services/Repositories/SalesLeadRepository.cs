using System.Data;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DBConnections;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Repositories
{
    public interface ISalesLeadRepository
    {
        Task<int> InsertAsync(SalesLead lead);
        Task<IEnumerable<SalesLead>> GetAllAsync();
    }

    /// <summary>
    /// Repository for SalesLead entity operations (tbl_SalesLeads).
    /// Uses the shared DBConnection/BaseRepository pattern for SQL injection protection.
    /// </summary>
    public class SalesLeadRepository : BaseRepository, ISalesLeadRepository
    {
        public SalesLeadRepository(DBConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<int> InsertAsync(SalesLead lead)
        {
            if (lead == null)
                throw new ArgumentNullException(nameof(lead));

            const string query = @"
                INSERT INTO [dbo].[tbl_SalesLeads]
                    ([school_name], [location], [school_type], [estimated_students], [website],
                     [contact_name], [contact_position], [email], [phone], [alternative_phone],
                     [lead_source], [interest_level], [interested_plan], [expected_close_date],
                     [assigned_agent], [budget_range], [notes], [status])
                VALUES
                    (@SchoolName, @Location, @SchoolType, @EstimatedStudents, @Website,
                     @ContactName, @ContactPosition, @Email, @Phone, @AlternativePhone,
                     @LeadSource, @InterestLevel, @InterestedPlan, @ExpectedCloseDate,
                     @AssignedAgent, @BudgetRange, @Notes, @Status)";

            var parameters = new[]
            {
                CreateParameter("@SchoolName", SanitizeString(lead.school_name, 150), SqlDbType.VarChar),
                CreateParameter("@Location", SanitizeString(lead.location, 150), SqlDbType.VarChar),
                CreateParameter("@SchoolType", string.IsNullOrWhiteSpace(lead.school_type) ? DBNull.Value : SanitizeString(lead.school_type, 50), SqlDbType.VarChar),
                CreateParameter("@EstimatedStudents", lead.estimated_students, SqlDbType.Int),
                CreateParameter("@Website", string.IsNullOrWhiteSpace(lead.website) ? DBNull.Value : SanitizeString(lead.website, 200), SqlDbType.VarChar),
                CreateParameter("@ContactName", SanitizeString(lead.contact_name, 150), SqlDbType.VarChar),
                CreateParameter("@ContactPosition", string.IsNullOrWhiteSpace(lead.contact_position) ? DBNull.Value : SanitizeString(lead.contact_position, 100), SqlDbType.VarChar),
                CreateParameter("@Email", SanitizeString(lead.email, 150), SqlDbType.VarChar),
                CreateParameter("@Phone", SanitizeString(lead.phone, 50), SqlDbType.VarChar),
                CreateParameter("@AlternativePhone", string.IsNullOrWhiteSpace(lead.alternative_phone) ? DBNull.Value : SanitizeString(lead.alternative_phone, 50), SqlDbType.VarChar),
                CreateParameter("@LeadSource", SanitizeString(lead.lead_source, 50), SqlDbType.VarChar),
                CreateParameter("@InterestLevel", SanitizeString(lead.interest_level, 20), SqlDbType.VarChar),
                CreateParameter("@InterestedPlan", string.IsNullOrWhiteSpace(lead.interested_plan) ? DBNull.Value : SanitizeString(lead.interested_plan, 20), SqlDbType.VarChar),
                CreateParameter("@ExpectedCloseDate", lead.expected_close_date.HasValue ? lead.expected_close_date.Value : DBNull.Value, SqlDbType.Date),
                CreateParameter("@AssignedAgent", string.IsNullOrWhiteSpace(lead.assigned_agent) ? DBNull.Value : SanitizeString(lead.assigned_agent, 100), SqlDbType.VarChar),
                CreateParameter("@BudgetRange", string.IsNullOrWhiteSpace(lead.budget_range) ? DBNull.Value : SanitizeString(lead.budget_range, 50), SqlDbType.VarChar),
                CreateParameter("@Notes", string.IsNullOrWhiteSpace(lead.notes) ? DBNull.Value : lead.notes, SqlDbType.Text),
                CreateParameter("@Status", SanitizeString(lead.status, 20), SqlDbType.VarChar)
            };

            return await ExecuteNonQueryAsync(query, parameters);
        }

        public async Task<IEnumerable<SalesLead>> GetAllAsync()
        {
            const string query = @"
                SELECT [lead_id], [school_name], [location], [school_type], [estimated_students], [website],
                       [contact_name], [contact_position], [email], [phone], [alternative_phone],
                       [lead_source], [interest_level], [interested_plan], [expected_close_date],
                       [assigned_agent], [budget_range], [notes], [status], [created_at]
                FROM [dbo].[tbl_SalesLeads]
                ORDER BY [created_at] DESC";

            var table = await ExecuteQueryAsync(query);

            var results = new List<SalesLead>();
            foreach (DataRow row in table.Rows)
            {
                results.Add(new SalesLead
                {
                    lead_id = Convert.ToInt32(row["lead_id"]),
                    school_name = row["school_name"].ToString() ?? string.Empty,
                    location = row["location"].ToString() ?? string.Empty,
                    school_type = row["school_type"] == DBNull.Value ? null : row["school_type"].ToString(),
                    estimated_students = row["estimated_students"] == DBNull.Value ? 0 : Convert.ToInt32(row["estimated_students"]),
                    website = row["website"] == DBNull.Value ? null : row["website"].ToString(),
                    contact_name = row["contact_name"].ToString() ?? string.Empty,
                    contact_position = row["contact_position"] == DBNull.Value ? null : row["contact_position"].ToString(),
                    email = row["email"].ToString() ?? string.Empty,
                    phone = row["phone"].ToString() ?? string.Empty,
                    alternative_phone = row["alternative_phone"] == DBNull.Value ? null : row["alternative_phone"].ToString(),
                    lead_source = row["lead_source"].ToString() ?? string.Empty,
                    interest_level = row["interest_level"].ToString() ?? string.Empty,
                    interested_plan = row["interested_plan"] == DBNull.Value ? null : row["interested_plan"].ToString(),
                    expected_close_date = row["expected_close_date"] == DBNull.Value ? null : Convert.ToDateTime(row["expected_close_date"]),
                    assigned_agent = row["assigned_agent"] == DBNull.Value ? null : row["assigned_agent"].ToString(),
                    budget_range = row["budget_range"] == DBNull.Value ? null : row["budget_range"].ToString(),
                    notes = row["notes"] == DBNull.Value ? null : row["notes"].ToString(),
                    status = row["status"].ToString() ?? "Active",
                    created_at = row["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["created_at"])
                });
            }

            return results;
        }
    }
}


