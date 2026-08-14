using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Models;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        /// <summary>
        /// Check whether new candidate registration is currently open.
        /// GET /api/registration/check-status
        /// Mirrors: BaseWorker.IsNewCandidateRegistrationStarted()
        /// </summary>
        [HttpGet("check-status")]
        public IActionResult CheckStatus()
        {
            var result = _registrationService.GetRegistrationStatus();
            return Ok(result);
        }

        /// <summary>
        /// Get all dropdown master data needed for the registration form.
        /// GET /api/registration/masters
        /// Returns: courses, genders, security questions
        /// </summary>
        [HttpGet("masters")]
        public IActionResult GetMasters()
        {
            var result = _registrationService.GetMasters();
            return Ok(result);
        }

        /// <summary>
        /// Register a new candidate.
        /// POST /api/registration/register
        /// Mirrors: NewRegistration.aspx RegisterCandidate()
        /// </summary>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (request == null)
                return BadRequest(new RegisterResponse { Success = false, Message = "Invalid request." });

            // Basic null / empty guards — same as old server-side validation
            if (string.IsNullOrWhiteSpace(request.CandidateName) ||
                string.IsNullOrWhiteSpace(request.MobileNo)      ||
                string.IsNullOrWhiteSpace(request.EMailID)       ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = "Please fill in all required fields."
                });

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = _registrationService.Register(request, ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get registration info to show on the confirmation page.
        /// GET /api/registration/info?loginId=2026101051
        /// Mirrors: ShowRegistrationInfo.aspx
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo([FromQuery] string loginId)
        {
            if (string.IsNullOrWhiteSpace(loginId))
                return BadRequest(new RegistrationInfoResponse { Found = false });

            var result = _registrationService.GetRegistrationInfo(loginId);

            if (!result.Found)
                return NotFound(new RegistrationInfoResponse { Found = false });

            return Ok(result);
        }
    }
}
