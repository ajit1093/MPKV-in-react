namespace MpkvCandidate.Api.Models
{
    // ── Forgot Login ID ──────────────────────────────────────────────────────

    /// <summary>
    /// Step 1 — enter candidate name + mobile → send OTP
    /// Mirrors: ForgotLoginID.aspx btnProceed_Click
    /// </summary>
    public class ForgotLoginIdRequest
    {
        public string CandidateName { get; set; } = string.Empty;
        public string MobileNo      { get; set; } = string.Empty;
    }

    /// <summary>
    /// Step 2 — verify OTP → reveal Login ID
    /// Mirrors: ForgotLoginID.aspx MobileVerified()
    /// </summary>
    public class ForgotLoginIdVerifyOtpRequest
    {
        public string CandidateName { get; set; } = string.Empty;
        public string MobileNo      { get; set; } = string.Empty;
        public string OTP           { get; set; } = string.Empty;
    }

    public class ForgotLoginIdResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
        // Returned only after OTP verified
        public string? LoginID { get; set; }
    }

    // ── Forgot Password — method selector ────────────────────────────────────

    /// <summary>
    /// Mirrors: ForgotPassword.aspx — choose reset method (1=SecurityQ, 2=EmailOTP, 3=MobileOTP)
    /// </summary>
    public class ForgotPasswordMethodRequest
    {
        /// <summary>1 = Security Question, 2 = OTP via Email, 3 = OTP via Mobile</summary>
        public int Method { get; set; }
    }

    // ── Reset by Security Question ────────────────────────────────────────────

    /// <summary>
    /// Mirrors: ResetPasswordBySecurityQuestion.aspx
    /// SP     : Account_CheckForgotPassword (with SecurityQuestionID + Answer)
    /// </summary>
    public class ResetBySecurityQuestionRequest
    {
        public string UserLoginID           { get; set; } = string.Empty;
        public short  SecurityQuestionID    { get; set; }
        public string SecurityQuestionAnswer{ get; set; } = string.Empty;
    }

    public class ResetBySecurityQuestionResponse
    {
        public bool   Success    { get; set; }
        public string Message    { get; set; } = string.Empty;
        /// <summary>
        /// Returned on success — frontend passes this as P1 to /reset-password
        /// Mirrors: Response.Redirect("ResetPassword.aspx?P1=...&P2=...GetHashCode()")
        /// </summary>
        public string? ResetToken { get; set; }
    }

    // ── Reset by OTP (Mobile or Email) ────────────────────────────────────────

    /// <summary>
    /// Mirrors: ResetPasswordByOTPMobileNo.aspx — step 1 verify login + mobile exist
    /// SP     : Account_CheckForgotPassword (UserLoginID + MobileNo)
    /// </summary>
    public class CheckAndSendOtpMobileRequest
    {
        public string UserLoginID { get; set; } = string.Empty;
        public string MobileNo    { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mirrors: ResetPasswordByOTPEMailID.aspx — step 1 verify login + email exist
    /// SP     : Account_CheckForgotPassword (UserLoginID + EMailID)
    /// </summary>
    public class CheckAndSendOtpEmailRequest
    {
        public string UserLoginID { get; set; } = string.Empty;
        public string EMailID     { get; set; } = string.Empty;
    }

    public class SendOtpResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Verify OTP entered by user — same for mobile and email flow
    /// </summary>
    public class VerifyOtpRequest
    {
        public string UserLoginID { get; set; } = string.Empty;
        public string OTP         { get; set; } = string.Empty;
        /// <summary>"Mobile" or "Email"</summary>
        public string Channel     { get; set; } = string.Empty;
        public string Contact     { get; set; } = string.Empty;   // mobile number or email id
    }

    public class VerifyOtpResponse
    {
        public bool   Success    { get; set; }
        public string Message    { get; set; } = string.Empty;
        /// <summary>Signed reset token — passed to /reset-password page as query param</summary>
        public string? ResetToken { get; set; }
    }

    // ── Reset Password (final step) ───────────────────────────────────────────

    /// <summary>
    /// Mirrors: ResetPassword.aspx — enter new password + confirm
    /// SP     : Account_GetUserID → Account_ResetPassword
    /// </summary>
    public class ResetPasswordRequest
    {
        public string ResetToken      { get; set; } = string.Empty;   // signed token = P1 (LoginID)
        public string NewPassword     { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── Masters (Security Questions) ─────────────────────────────────────────

    public class AccountMastersResponse
    {
        public List<DropdownItem> SecurityQuestions { get; set; } = new();
    }
}
