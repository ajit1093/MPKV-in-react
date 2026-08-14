using Dapper;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;

namespace MpkvCandidate.Api.Services
{
    public interface IAccountRecoveryService
    {
        AccountMastersResponse          GetMasters();

        // Forgot Login ID — Step 1: verify name+mobile, send OTP
        ForgotLoginIdResponse           SendForgotLoginIdOtp(ForgotLoginIdRequest request);

        // Forgot Login ID — Step 2: verify OTP, reveal Login ID
        ForgotLoginIdResponse           VerifyForgotLoginIdOtp(ForgotLoginIdVerifyOtpRequest request);

        ResetBySecurityQuestionResponse CheckBySecurityQuestion(ResetBySecurityQuestionRequest request);
        SendOtpResponse                 CheckAndSendOtpMobile(CheckAndSendOtpMobileRequest request);
        SendOtpResponse                 CheckAndSendOtpEmail(CheckAndSendOtpEmailRequest request);
        VerifyOtpResponse               VerifyOtp(VerifyOtpRequest request);
        ResetPasswordResponse           ResetPassword(ResetPasswordRequest request, string ipAddress);
    }

    public class AccountRecoveryService : IAccountRecoveryService
    {
        private readonly DbAccess          _db;
        private readonly IMessagingService _messaging;
        private readonly IConfiguration   _config;

        public AccountRecoveryService(DbAccess db, IMessagingService messaging, IConfiguration config)
        {
            _db        = db;
            _messaging = messaging;
            _config    = config;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/account/masters
        // SP: Base_GetMasterTableList
        // ─────────────────────────────────────────────────────────────────────
        public AccountMastersResponse GetMasters()
        {
            var response = new AccountMastersResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",        "Master_SecurityQuestion");
                param.Add("@DataValueField",   "SecurityQuestionID");
                param.Add("@DataTextField",    "SecurityQuestion");
                param.Add("@ParentField",      "");
                param.Add("@ParentFieldValue", "");
                param.Add("@OrderByFields",    "SecurityQuestion");

                var dt = _db.GetDataTable("Base_GetMasterTableList", param);

                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.SecurityQuestions.Add(new DropdownItem
                        {
                            Value = row[0].ToString()!,
                            Text  = row[1].ToString()!
                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMasters error: {ex.Message}");
            }
            return response;
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/forgot-login-id/send-otp   — STEP 1
        // Old flow: enter name+mobile → Account_GetUserLoginID → if found →
        //           Base_GetOTP → MessagingHelperMsg91.SendSMS → show OTP modal
        // SP params: @UserName, @MobileNo  (Repository.Account.cs GetUserLoginID)
        // ─────────────────────────────────────────────────────────────────────
        public ForgotLoginIdResponse SendForgotLoginIdOtp(ForgotLoginIdRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CandidateName) ||
                    string.IsNullOrWhiteSpace(request.MobileNo))
                    return new ForgotLoginIdResponse
                    {
                        Success = false,
                        Message = "Please enter Candidate Name and Mobile Number."
                    };

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.MobileNo.Trim(), @"^\d{10}$"))
                    return new ForgotLoginIdResponse
                    {
                        Success = false,
                        Message = "Please enter a valid 10-digit Mobile Number."
                    };

                // Step 1 — verify name + mobile exist
                var idParam = new DynamicParameters();
                idParam.Add("@UserName", request.CandidateName.Trim().ToUpper());
                idParam.Add("@MobileNo", request.MobileNo.Trim());

                var loginId = _db.ExecuteScalar("Account_GetUserLoginID", idParam)?.ToString() ?? "";

                if (loginId.Length == 0)
                    return new ForgotLoginIdResponse
                    {
                        Success = false,
                        Message = "No Record Found. Please check your Candidate Name and Mobile Number."
                    };

                // Step 2 — generate OTP and store in DB
                // Same as old: string OTP = new BaseWorker().GetOTP(MobileNo, "ForgotLoginID", Helper.GenerateOTP(4))
                var otpVal   = GenerateOTP(4);
                var otpParam = new DynamicParameters();
                otpParam.Add("@MobileNo", request.MobileNo.Trim());
                otpParam.Add("@Purpose",  "ForgotLoginID");
                otpParam.Add("@OTP",      otpVal);

                var storedOtp = _db.ExecuteScalar("Base_GetOTP", otpParam)?.ToString() ?? otpVal;

                // Step 3 — get SMS template from DB (same as old BaseWorker().GetEMailSMS("ForgotLoginID","S"))
                var smsParam = new DynamicParameters();
                smsParam.Add("@Purpose",     "ForgotLoginID");
                smsParam.Add("@MessageType", "S");
                smsParam.Add("@Param1", ""); smsParam.Add("@Param2", ""); smsParam.Add("@Param3", "");
                smsParam.Add("@Param4", ""); smsParam.Add("@Param5", "");

                var smsDt      = _db.GetDataTable("Base_GetEMailSMS", smsParam);
                var templateId = smsDt?.Rows.Count > 0 ? smsDt.Rows[0]["TemplateID"].ToString() ?? "" : "";

                // Step 4 — send SMS with OTP (same as old ucMobileOTPVerification.SendOTP())
                _ = _messaging.SendSmsAsync(request.MobileNo.Trim(), storedOtp, templateId);

                var maskedMobile = MaskMobile(request.MobileNo.Trim());
                return new ForgotLoginIdResponse
                {
                    Success = true,
                    Message = $"OTP has been sent to Mobile No. {maskedMobile}. Please enter OTP to get your Login ID."
                };
            }
            catch (Exception ex)
            {
                return new ForgotLoginIdResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/forgot-login-id/verify-otp   — STEP 2
        // Old flow (MobileVerified callback): verify OTP via Base_SaveOTPVerificationStatus
        //   → if Y → show Login ID in AlertBox
        // SP params: @MobileNo, @Purpose, @OTP  (Repository.Base.cs SaveOTPVerificationStatus)
        // ─────────────────────────────────────────────────────────────────────
        public ForgotLoginIdResponse VerifyForgotLoginIdOtp(ForgotLoginIdVerifyOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OTP))
                    return new ForgotLoginIdResponse { Success = false, Message = "Please enter the OTP." };

                // Verify OTP — same SP as old BaseWorker().SaveOTPVerificationStatus()
                var param = new DynamicParameters();
                param.Add("@MobileNo", request.MobileNo.Trim());
                param.Add("@Purpose",  "ForgotLoginID");
                param.Add("@OTP",      request.OTP.Trim());

                var result = _db.ExecuteScalar("Base_SaveOTPVerificationStatus", param)?.ToString() ?? "";

                if (result.ToUpper() != "Y")
                    return new ForgotLoginIdResponse
                    {
                        Success = false,
                        Message = "Invalid OTP. Please try again."
                    };

                // OTP verified — now get Login ID to show (same as old MobileVerified → ucAlertBox)
                var idParam = new DynamicParameters();
                idParam.Add("@UserName", request.CandidateName.Trim().ToUpper());
                idParam.Add("@MobileNo", request.MobileNo.Trim());

                var loginId = _db.ExecuteScalar("Account_GetUserLoginID", idParam)?.ToString() ?? "";

                return new ForgotLoginIdResponse
                {
                    Success = true,
                    Message = "OTP verified successfully.",
                    LoginID = loginId
                };
            }
            catch (Exception ex)
            {
                return new ForgotLoginIdResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/reset-password-by-security-question
        // SP: Account_CheckForgotPassword
        // Params: @UserLoginID, @SecurityQuestionID, @SecurityQuestionAnswer
        // ─────────────────────────────────────────────────────────────────────
        public ResetBySecurityQuestionResponse CheckBySecurityQuestion(ResetBySecurityQuestionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserLoginID))
                    return new ResetBySecurityQuestionResponse { Success = false, Message = "Please enter your Login ID." };
                if (request.SecurityQuestionID <= 0)
                    return new ResetBySecurityQuestionResponse { Success = false, Message = "Please select a Security Question." };
                if (string.IsNullOrWhiteSpace(request.SecurityQuestionAnswer))
                    return new ResetBySecurityQuestionResponse { Success = false, Message = "Please enter your Security Question Answer." };

                var param = new DynamicParameters();
                param.Add("@UserLoginID",            request.UserLoginID.Trim().ToUpper());
                param.Add("@SecurityQuestionID",     request.SecurityQuestionID);
                param.Add("@SecurityQuestionAnswer", request.SecurityQuestionAnswer.Trim().ToUpper());

                var dt = _db.GetDataTable("Account_CheckForgotPassword", param);

                if (dt == null || dt.Rows.Count == 0)
                    return new ResetBySecurityQuestionResponse { Success = false, Message = "No record found." };

                var loginId  = dt.Rows[0]["UserLoginID"].ToString() ?? "";
                var errorMsg = dt.Rows[0]["ErrorMessage"].ToString() ?? "";

                if (loginId.Length == 0)
                    return new ResetBySecurityQuestionResponse
                    {
                        Success = false,
                        Message = errorMsg.Length > 0 ? errorMsg : "Invalid Login ID, Security Question or Answer."
                    };

                return new ResetBySecurityQuestionResponse
                {
                    Success    = true,
                    Message    = "Verification successful.",
                    ResetToken = BuildResetToken(loginId)
                };
            }
            catch (Exception ex)
            {
                return new ResetBySecurityQuestionResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/send-otp/mobile
        // SP: Account_CheckForgotPasswordByOTP  (@UserLoginID, @EMailID, @MobileNo)
        //     Base_GetOTP  (@MobileNo, @Purpose, @OTP)
        // Old: MessagingHelperMsg91.SendSMS with templateId from Base_GetEMailSMS
        // ─────────────────────────────────────────────────────────────────────
        public SendOtpResponse CheckAndSendOtpMobile(CheckAndSendOtpMobileRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserLoginID))
                    return new SendOtpResponse { Success = false, Message = "Please enter your Login ID." };
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.MobileNo?.Trim() ?? "", @"^\d{10}$"))
                    return new SendOtpResponse { Success = false, Message = "Please enter a valid 10-digit Mobile Number." };

                // Verify loginId + mobile
                var checkParam = new DynamicParameters();
                checkParam.Add("@UserLoginID", request.UserLoginID.Trim().ToUpper());
                checkParam.Add("@EMailID",     "");
                checkParam.Add("@MobileNo",    request.MobileNo.Trim());

                var dt = _db.GetDataTable("Account_CheckForgotPasswordByOTP", checkParam);

                if (dt == null || dt.Rows.Count == 0)
                    return new SendOtpResponse { Success = false, Message = "No record found." };

                var loginId  = dt.Rows[0]["UserLoginID"].ToString() ?? "";
                var errorMsg = dt.Rows[0]["ErrorMessage"].ToString() ?? "";

                if (loginId.Length == 0)
                    return new SendOtpResponse
                    {
                        Success = false,
                        Message = errorMsg.Length > 0 ? errorMsg : "Invalid Login ID or Mobile Number."
                    };

                // Generate + store OTP
                var otpVal   = GenerateOTP(4);
                var otpParam = new DynamicParameters();
                otpParam.Add("@MobileNo", request.MobileNo.Trim());
                otpParam.Add("@Purpose",  "ResetPassword");
                otpParam.Add("@OTP",      otpVal);

                var storedOtp = _db.ExecuteScalar("Base_GetOTP", otpParam)?.ToString() ?? otpVal;

                // Get SMS template from DB
                var smsParam = new DynamicParameters();
                smsParam.Add("@Purpose", "ResetPassword"); smsParam.Add("@MessageType", "S");
                smsParam.Add("@Param1", ""); smsParam.Add("@Param2", ""); smsParam.Add("@Param3", "");
                smsParam.Add("@Param4", ""); smsParam.Add("@Param5", "");
                var smsDt      = _db.GetDataTable("Base_GetEMailSMS", smsParam);
                var templateId = smsDt?.Rows.Count > 0 ? smsDt.Rows[0]["TemplateID"].ToString() ?? "" : "";

                // Send SMS — same as old ucMobileOTPVerification.SendOTP()
                _ = _messaging.SendSmsAsync(request.MobileNo.Trim(), storedOtp, templateId);

                return new SendOtpResponse
                {
                    Success = true,
                    Message = $"OTP has been sent to Mobile No. {MaskMobile(request.MobileNo.Trim())}. Please check your SMS."
                };
            }
            catch (Exception ex)
            {
                return new SendOtpResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/send-otp/email
        // SP: Account_CheckForgotPasswordByOTP  (@UserLoginID, @EMailID, @MobileNo)
        //     Base_GetOTP  (@MobileNo=emailId, @Purpose, @OTP)
        // Old: Mailer.SendEMail with subject/message from Base_GetEMailSMS
        // ─────────────────────────────────────────────────────────────────────
        public SendOtpResponse CheckAndSendOtpEmail(CheckAndSendOtpEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserLoginID))
                    return new SendOtpResponse { Success = false, Message = "Please enter your Login ID." };
                if (string.IsNullOrWhiteSpace(request.EMailID))
                    return new SendOtpResponse { Success = false, Message = "Please enter your Email ID." };

                // Verify loginId + email
                var checkParam = new DynamicParameters();
                checkParam.Add("@UserLoginID", request.UserLoginID.Trim().ToUpper());
                checkParam.Add("@EMailID",     request.EMailID.Trim().ToLower());
                checkParam.Add("@MobileNo",    "");

                var dt = _db.GetDataTable("Account_CheckForgotPasswordByOTP", checkParam);

                if (dt == null || dt.Rows.Count == 0)
                    return new SendOtpResponse { Success = false, Message = "No record found." };

                var loginId  = dt.Rows[0]["UserLoginID"].ToString() ?? "";
                var errorMsg = dt.Rows[0]["ErrorMessage"].ToString() ?? "";

                if (loginId.Length == 0)
                    return new SendOtpResponse
                    {
                        Success = false,
                        Message = errorMsg.Length > 0 ? errorMsg : "Invalid Login ID or Email ID."
                    };

                // Generate + store OTP — email keyed by EMailID in @MobileNo column (same as old)
                var otpVal   = GenerateOTP(4);
                var otpParam = new DynamicParameters();
                otpParam.Add("@MobileNo", request.EMailID.Trim().ToLower());
                otpParam.Add("@Purpose",  "ResetPassword");
                otpParam.Add("@OTP",      otpVal);

                var storedOtp = _db.ExecuteScalar("Base_GetOTP", otpParam)?.ToString() ?? otpVal;

                // Get email template from DB — same as old BaseWorker().GetEMailSMS("ResetPassword","E")
                var emailParam = new DynamicParameters();
                emailParam.Add("@Purpose", "ResetPassword"); emailParam.Add("@MessageType", "E");
                emailParam.Add("@Param1", ""); emailParam.Add("@Param2", ""); emailParam.Add("@Param3", "");
                emailParam.Add("@Param4", ""); emailParam.Add("@Param5", "");
                var emailDt  = _db.GetDataTable("Base_GetEMailSMS", emailParam);

                string subject  = emailDt?.Rows.Count > 0 ? emailDt.Rows[0]["Subject"].ToString()  ?? "Reset Password OTP" : "Reset Password OTP";
                string msgBody  = emailDt?.Rows.Count > 0 ? emailDt.Rows[0]["Message"].ToString() ?? "" : "";

                // Replace ##OTP## placeholder — same as old EMailMessage.Replace("##OTP##", OTP)
                if (msgBody.Contains("##OTP##"))
                    msgBody = msgBody.Replace("##OTP##", storedOtp);
                else
                    msgBody = $"<p>Your OTP for Reset Password is: <strong>{storedOtp}</strong></p><p>This OTP is valid for 10 minutes.</p>";

                // Send email — same as old Mailer.SendEMail() with purpose="ResetPassword"
                _ = _messaging.SendEmailAsync(request.EMailID.Trim(), subject, msgBody, "ResetPassword");

                return new SendOtpResponse
                {
                    Success = true,
                    Message = $"OTP has been sent to E-Mail ID {MaskEmail(request.EMailID.Trim())}. Please check your inbox."
                };
            }
            catch (Exception ex)
            {
                return new SendOtpResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/verify-otp
        // SP: Base_SaveOTPVerificationStatus  (@MobileNo, @Purpose, @OTP)
        // Returns "Y" on success — same as old VerifyOTP() in user controls
        // ─────────────────────────────────────────────────────────────────────
        public VerifyOtpResponse VerifyOtp(VerifyOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OTP))
                    return new VerifyOtpResponse { Success = false, Message = "Please enter the OTP." };

                var param = new DynamicParameters();
                param.Add("@MobileNo", request.Contact.Trim()); // mobile or email — stored under same @MobileNo key
                param.Add("@Purpose",  "ResetPassword");
                param.Add("@OTP",      request.OTP.Trim());

                var result = _db.ExecuteScalar("Base_SaveOTPVerificationStatus", param)?.ToString() ?? "";

                if (result.ToUpper() != "Y")
                    return new VerifyOtpResponse
                    {
                        Success = false,
                        Message = "Invalid OTP. Please try again."
                    };

                return new VerifyOtpResponse
                {
                    Success    = true,
                    Message    = "OTP verified successfully.",
                    ResetToken = BuildResetToken(request.UserLoginID)
                };
            }
            catch (Exception ex)
            {
                return new VerifyOtpResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/account/reset-password
        // SPs: Account_GetUserID (@UserLoginID) → Account_ResetPassword
        // ─────────────────────────────────────────────────────────────────────
        public ResetPasswordResponse ResetPassword(ResetPasswordRequest request, string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ResetToken))
                    return new ResetPasswordResponse { Success = false, Message = "Invalid or missing reset token." };
                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return new ResetPasswordResponse { Success = false, Message = "Please enter a new password." };
                if (request.NewPassword != request.ConfirmPassword)
                    return new ResetPasswordResponse { Success = false, Message = "New Password and Confirm New Password should be same." };

                var loginId = ValidateResetToken(request.ResetToken);
                if (string.IsNullOrEmpty(loginId))
                    return new ResetPasswordResponse { Success = false, Message = "Invalid or expired reset link." };

                var idParam = new DynamicParameters();
                idParam.Add("@UserLoginID", loginId);
                var userIdObj = _db.ExecuteScalar("Account_GetUserID", idParam);
                if (userIdObj == null)
                    return new ResetPasswordResponse { Success = false, Message = "User not found." };

                long userId = Convert.ToInt64(userIdObj);

                string encodedPassword = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(request.NewPassword));

                var resetParam = new DynamicParameters();
                resetParam.Add("@UserID",              userId);
                resetParam.Add("@NewPassword",         encodedPassword);
                resetParam.Add("@LoggedInUserLoginID", "");
                resetParam.Add("@IPAddress",           ipAddress);

                var result = _db.ExecuteScalar("Account_ResetPassword", resetParam)?.ToString() ?? "";

                if (result.ToUpper() == "Y")
                    return new ResetPasswordResponse { Success = true, Message = "Password changed successfully." };

                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Password has not reset. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new ResetPasswordResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────
        private static string BuildResetToken(string loginId)
        {
            var raw = $"{loginId}|{loginId.GetHashCode()}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        }

        private static string ValidateResetToken(string token)
        {
            try
            {
                var raw   = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = raw.Split('|');
                if (parts.Length != 2) return "";
                return parts[0].GetHashCode().ToString() == parts[1] ? parts[0] : "";
            }
            catch { return ""; }
        }

        private static string GenerateOTP(int length)
        {
            var rng = new Random();
            return string.Concat(Enumerable.Range(0, length).Select(_ => rng.Next(0, 10).ToString()));
        }

        private static string MaskMobile(string mobile) =>
            mobile.Length >= 10 ? "XXXXXX" + mobile[^4..] : mobile;

        private static string MaskEmail(string email)
        {
            var idx = email.IndexOf('@');
            if (idx <= 1) return email;
            return email[0] + new string('*', Math.Min(idx - 1, 2)) + email[idx..];
        }
    }
}
