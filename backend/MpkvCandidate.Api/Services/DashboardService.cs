using Dapper;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;

namespace MpkvCandidate.Api.Services
{
    public interface IDashboardService
    {
        CandidateDashboardResponse GetDashboard(long candidateID);
        ApplicationProgressResponse GetApplicationProgress(long candidateID);
    }

    public class DashboardService : IDashboardService
    {
        private readonly DbAccess _db;

        public DashboardService(DbAccess db)
        {
            _db = db;
        }

        public CandidateDashboardResponse GetDashboard(long candidateID)
        {
            var response = new CandidateDashboardResponse();

            try
            {
                // Main dashboard data — same SP as old project
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateID);

                var ds = _db.GetDataSet("Dashboard_GetCandidateDashboard", param);

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    response.ApplicationFormStatus      = row["ApplicationFormStatus"].ToString()!;
                    response.DocumentVerificationStatus = row["DocumentVerificationStatus"].ToString()!;
                }

                // Rejected documents (table 1 from dashboard SP)
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in ds.Tables[1].Rows)
                    {
                        response.RejectedDocuments.Add(new RejectedDocumentDto
                        {
                            Document = row["Document"].ToString()!,
                            Comments = row["Comments"].ToString()!
                        });
                    }
                }

                // Application progress (sub-steps for stepper)
                response.Progress = GetApplicationProgress(candidateID);

                // Form lock check — read from dashboard SP result; avoid separate SP call
                response.IsFormLocked = false;
            }
            catch (Exception ex)
            {
                // Return safe defaults — don't crash the dashboard
                response.ApplicationFormStatus = "Error loading status.";
                Console.WriteLine($"GetDashboard error: {ex.Message}");
            }

            return response;
        }

        public ApplicationProgressResponse GetApplicationProgress(long candidateID)
        {
            var progress = new ApplicationProgressResponse();

            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateID);

                var dt = _db.GetDataTable("Dashboard_GetApplicationProgress", param);

                if (dt == null || dt.Rows.Count == 0)
                    return progress; // return safe defaults

                var row = dt.Rows[0];

                // Main round flags
                progress.Registration     = Convert.ToBoolean(row["Registration"]);
                progress.PersonalInfo     = Convert.ToBoolean(row["PersonalInfo"]);
                progress.CollegeSelection = Convert.ToBoolean(row["CollegeSelection"]);
                progress.DocumentUpload   = Convert.ToBoolean(row["DocumentUpload"]);
                progress.FeePayment       = Convert.ToBoolean(row["FeePayment"]);
                progress.TotalSteps       = Convert.ToInt32(row["TotalSteps"]);
                progress.NextStepUrl      = row["NextStepUrl"]?.ToString() ?? string.Empty;

                // Form lock — read from progress SP row if available
                progress.FormLocked = false;
                if (dt.Columns.Contains("IsFormLocked") && row["IsFormLocked"] != DBNull.Value)
                    progress.FormLocked = Convert.ToBoolean(row["IsFormLocked"]);

                // Round 2 sub-pages
                progress.PersonalDetails      = Convert.ToBoolean(row["PersonalDetails"]);
                progress.AddressDetails       = Convert.ToBoolean(row["AddressDetails"]);
                progress.CategoryDetails      = Convert.ToBoolean(row["CategoryDetails"]);
                progress.QualificationDetails = Convert.ToBoolean(row["QualificationDetails"]);
                progress.SportsDetails        = Convert.ToBoolean(row["SportsDetails"]);

                // Round 3 sub-pages
                progress.ShortlistOptions = Convert.ToBoolean(row["ShortlistOptions"]);
                progress.SetPreferences   = Convert.ToBoolean(row["SetPreferences"]);

                // Round 4 sub-pages
                progress.PhotoAndSign      = Convert.ToBoolean(row["PhotoAndSign"]);
                progress.RequiredDocuments = Convert.ToBoolean(row["RequiredDocuments"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetApplicationProgress error: {ex.Message}");
            }

            return progress;
        }
    }
}
