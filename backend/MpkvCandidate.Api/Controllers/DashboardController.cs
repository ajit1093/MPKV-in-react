using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // all endpoints require a valid JWT token
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Returns full candidate dashboard data including progress stepper.
        /// GET /api/dashboard
        /// </summary>
        [HttpGet]
        public IActionResult GetDashboard()
        {
            long candidateID = GetCandidateID();
            if (candidateID <= 0)
                return Unauthorized(new { message = "Invalid token — candidate ID not found." });

            var result = _dashboardService.GetDashboard(candidateID);
            return Ok(result);
        }

        /// <summary>
        /// Returns only the progress stepper data (lighter call for refresh).
        /// GET /api/dashboard/progress
        /// </summary>
        [HttpGet("progress")]
        public IActionResult GetProgress()
        {
            long candidateID = GetCandidateID();
            if (candidateID <= 0)
                return Unauthorized(new { message = "Invalid token — candidate ID not found." });

            var result = _dashboardService.GetApplicationProgress(candidateID);
            return Ok(result);
        }

        // ── Helper: reads CandidateID from JWT sub claim ─────────────────────
        private long GetCandidateID()
        {
            var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
            return long.TryParse(sub, out var id) ? id : 0;
        }
    }
}
