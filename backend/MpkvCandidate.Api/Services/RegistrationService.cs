using Dapper;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;

namespace MpkvCandidate.Api.Services
{
    public interface IRegistrationService
    {
        RegistrationStatusResponse  GetRegistrationStatus();
        RegistrationMastersResponse GetMasters();
        RegisterResponse            Register(RegisterRequest request, string ipAddress);
        RegistrationInfoResponse    GetRegistrationInfo(string loginId);
    }

    public class RegistrationService : IRegistrationService
    {
        private readonly DbAccess _db;

        public RegistrationService(DbAccess db)
        {
            _db = db;
        }

        // ── GET /api/registration/check-status ───────────────────────────────
        // Mirrors: BaseWorker.IsNewCandidateRegistrationStarted()
        // SP     : Base_IsNewCandidateRegistrationStarted
        public RegistrationStatusResponse GetRegistrationStatus()
        {
            try
            {
                var result = _db.ExecuteScalar("Base_IsNewCandidateRegistrationStarted");
                return new RegistrationStatusResponse
                {
                    IsOpen = result != null && Convert.ToBoolean(result)
                };
            }
            catch
            {
                return new RegistrationStatusResponse { IsOpen = false };
            }
        }

        // ── GET /api/registration/masters ────────────────────────────────────
        // Mirrors: NewRegistration.aspx LoadMasters()
        // SPs    : Base_GetMasterTableList (3 calls — Course, Gender, SecurityQuestion)
        public RegistrationMastersResponse GetMasters()
        {
            var response = new RegistrationMastersResponse();

            // Courses — same as Base_GetMasterCourse but via GetMasterTableList
            // Old code uses GetMasterCourse() → Base_GetMasterCourse SP
            try
            {
                var dt = _db.GetDataTable("Base_GetMasterCourse");
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Courses.Add(new DropdownItem
                        {
                            Value = row[0].ToString()!,
                            Text  = row[1].ToString()!
                        });
            }
            catch { /* return empty list */ }

            // Genders — Master_Gender table
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",       "Master_Gender");
                param.Add("@DataValueField",  "GenderCode");
                param.Add("@DataTextField",   "Gender");
                param.Add("@ParentField",     "");
                param.Add("@ParentFieldValue","");
                param.Add("@OrderByFields",   "GenderCode");

                var dt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Genders.Add(new DropdownItem
                        {
                            Value = row[0].ToString()!,
                            Text  = row[1].ToString()!
                        });
            }
            catch { }

            // Security Questions — Master_SecurityQuestion table
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",       "Master_SecurityQuestion");
                param.Add("@DataValueField",  "SecurityQuestionID");
                param.Add("@DataTextField",   "SecurityQuestion");
                param.Add("@ParentField",     "");
                param.Add("@ParentFieldValue","");
                param.Add("@OrderByFields",   "SecurityQuestion");

                var dt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.SecurityQuestions.Add(new DropdownItem
                        {
                            Value = row[0].ToString()!,
                            Text  = row[1].ToString()!
                        });
            }
            catch { }

            return response;
        }

        // ── POST /api/registration/register ──────────────────────────────────
        // Mirrors: NewRegistration.aspx RegisterCandidate()
        // SPs    : ApplicationForm_IsApplicationFormAlreadyRegisteredUsingThisMobileNo
        //          ApplicationForm_IsApplicationFormAlreadyRegisteredUsingThisEMailID
        //          ApplicationForm_RegisterCandidate
        //          Base_GetApplicationID  (to get LoginID for info page)
        public RegisterResponse Register(RegisterRequest request, string ipAddress)
        {
            try
            {
                // 1. Duplicate mobile check
                var mobileParam = new DynamicParameters();
                mobileParam.Add("@CandidateID", 0L);
                mobileParam.Add("@MobileNo",    request.MobileNo.Trim());

                var mobileResult = _db.ExecuteScalar(
                    "ApplicationForm_IsApplicationFormAlreadyRegisteredUsingThisMobileNo",
                    mobileParam);

                if (mobileResult != null && Convert.ToBoolean(mobileResult))
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = $"Application Form using Mobile Number {request.MobileNo} is already registered. Please use a different mobile number."
                    };

                // 2. Duplicate email check
                var emailParam = new DynamicParameters();
                emailParam.Add("@CandidateID", 0L);
                emailParam.Add("@EMailID",     request.EMailID.Trim().ToLower());

                var emailResult = _db.ExecuteScalar(
                    "ApplicationForm_IsApplicationFormAlreadyRegisteredUsingThisEMailID",
                    emailParam);

                if (emailResult != null && Convert.ToBoolean(emailResult))
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = $"Application Form using E-Mail ID {request.EMailID} is already registered. Please use a different email address."
                    };

                // 3. Parse DOB — frontend sends "dd/MM/yyyy"
                if (!DateTime.TryParseExact(
                        request.DOB.Trim(),
                        new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dob))
                    return new RegisterResponse { Success = false, Message = "Invalid date of birth format. Use dd/MM/yyyy." };

                // 4. Encode password — same as CommonHelper.Base64Encrypt() in old project
                string encodedPassword = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(request.Password));

                // 5. Register candidate
                var regParam = new DynamicParameters();
                regParam.Add("@AppliedCourseID",        request.AppliedCourseID);
                regParam.Add("@CandidateName",          request.CandidateName.Trim().ToUpper());
                regParam.Add("@FatherName",             request.FatherName.Trim().ToUpper());
                regParam.Add("@MotherName",             request.MotherName.Trim().ToUpper());
                regParam.Add("@GenderCode",             request.GenderCode);
                regParam.Add("@DOB",                    dob);
                regParam.Add("@MobileNo",               request.MobileNo.Trim());
                regParam.Add("@EMailID",                request.EMailID.Trim().ToLower());
                regParam.Add("@SecurityQuestionID",     request.SecurityQuestionID);
                regParam.Add("@SecurityQuestionAnswer", request.SecurityQuestionAnswer.Trim().ToUpper());
                regParam.Add("@Password",               encodedPassword);
                regParam.Add("@UserLoginID",            "");
                regParam.Add("@IPAddress",              ipAddress);
                regParam.Add("@PageCode",               "Registration");

                var returnValue = _db.ExecuteScalar("ApplicationForm_RegisterCandidate", regParam)
                                    ?.ToString() ?? "";

                // SP returns 10-digit Application ID on success, or error message
                if (returnValue.Length == 10)
                {
                    return new RegisterResponse
                    {
                        Success       = true,
                        Message       = "Registration successful.",
                        LoginID       = returnValue,
                        CandidateName = request.CandidateName.Trim().ToUpper()
                    };
                }

                return new RegisterResponse
                {
                    Success = false,
                    Message = returnValue.Length > 0
                        ? returnValue
                        : "Registration failed. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = $"Registration error: {ex.Message}"
                };
            }
        }

        // ── GET /api/registration/info?loginId=X ─────────────────────────────
        // Mirrors: ShowRegistrationInfo.aspx — shows Application ID + Candidate Name
        // SP     : Account_GetUserName (via AccountWorker.GetUserName)
        public RegistrationInfoResponse GetRegistrationInfo(string loginId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginId))
                    return new RegistrationInfoResponse { Found = false };

                var param = new DynamicParameters();
                param.Add("@UserLoginID", loginId.Trim());

                var name = _db.ExecuteScalar("Account_GetUserName", param)?.ToString() ?? "";

                if (string.IsNullOrEmpty(name))
                    return new RegistrationInfoResponse { Found = false };

                return new RegistrationInfoResponse
                {
                    Found         = true,
                    LoginID       = loginId.Trim(),
                    CandidateName = name
                };
            }
            catch
            {
                return new RegistrationInfoResponse { Found = false };
            }
        }
    }
}
