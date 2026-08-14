using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Models;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountRecoveryController : ControllerBase
    {
        private readonly IAccountRecoveryService _accountService;

        public AccountRecoveryController(IAccountRecoveryService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Security questions dropdown.
        /// GET /api/account/masters
        /// </summary>
        [HttpGet("masters")]
        public IActionResult GetMasters()
        {
            var result = _accountService.GetMasters();
            return Ok(result);
        }

        /// <summary>
        /// Step 1 — verify name+mobile, send OTP to mobile.
        /// POST /api/account/forgot-login-id/send-otp
        /// Mirrors: ForgotLoginID.aspx btnProceed_Click
        /// </summary>
        [HttpPost("forgot-login-id/send-otp")]
        public IActionResult ForgotLoginIdSendOtp([FromBody] ForgotLoginIdRequest request)
        {
            if (request == null)
                return BadRequest(new ForgotLoginIdResponse { Success = false, Message = "Invalid request." });
            var result = _accountService.SendForgotLoginIdOtp(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Step 2 — verify OTP, reveal Login ID.
        /// POST /api/account/forgot-login-id/verify-otp
        /// Mirrors: ForgotLoginID.aspx MobileVerified()
        /// </summary>
        [HttpPost("forgot-login-id/verify-otp")]
        public IActionResult ForgotLoginIdVerifyOtp([FromBody] ForgotLoginIdVerifyOtpRequest request)
        {
            if (request == null)
                return BadRequest(new ForgotLoginIdResponse { Success = false, Message = "Invalid request." });
            var result = _accountService.VerifyForgotLoginIdOtp(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Reset password by Security Question — verify and get reset token.
        /// POST /api/account/reset-password-by-security-question
        /// Mirrors: ResetPasswordBySecurityQuestion.aspx btnProceed_Click
        /// </summary>
        [HttpPost("reset-password-by-security-question")]
        public IActionResult ResetBySecurityQuestion([FromBody] ResetBySecurityQuestionRequest request)
        {
            if (request == null)
                return BadRequest(new ResetBySecurityQuestionResponse { Success = false, Message = "Invalid request." });

            var result = _accountService.CheckBySecurityQuestion(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Send OTP to Mobile for password reset.
        /// POST /api/account/send-otp/mobile
        /// Mirrors: ResetPasswordByOTPMobileNo.aspx btnProceed_Click
        /// </summary>
        [HttpPost("send-otp/mobile")]
        public IActionResult SendOtpMobile([FromBody] CheckAndSendOtpMobileRequest request)
        {
            if (request == null)
                return BadRequest(new SendOtpResponse { Success = false, Message = "Invalid request." });

            var result = _accountService.CheckAndSendOtpMobile(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Send OTP to Email for password reset.
        /// POST /api/account/send-otp/email
        /// Mirrors: ResetPasswordByOTPEMailID.aspx btnProceed_Click
        /// </summary>
        [HttpPost("send-otp/email")]
        public IActionResult SendOtpEmail([FromBody] CheckAndSendOtpEmailRequest request)
        {
            if (request == null)
                return BadRequest(new SendOtpResponse { Success = false, Message = "Invalid request." });

            var result = _accountService.CheckAndSendOtpEmail(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Verify OTP (mobile or email) and get reset token.
        /// POST /api/account/verify-otp
        /// Mirrors: ucMobileOTPVerification.MobileVerified / ucEMailOTPVerification.EMailVerified
        /// </summary>
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (request == null)
                return BadRequest(new VerifyOtpResponse { Success = false, Message = "Invalid request." });

            var result = _accountService.VerifyOtp(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Final step — reset password using token.
        /// POST /api/account/reset-password
        /// Mirrors: ResetPassword.aspx btnChangePassword_Click
        /// </summary>
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null)
                return BadRequest(new ResetPasswordResponse { Success = false, Message = "Invalid request." });

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = _accountService.ResetPassword(request, ip);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
