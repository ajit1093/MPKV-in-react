using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Models;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Candidate login — returns a JWT token on success.
        /// POST /api/auth/login
        /// Body: { "userLoginID": "2026101051", "userPassword": "xxxxx" }
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserLoginID) ||
                string.IsNullOrWhiteSpace(request.UserPassword))
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Login ID and password are required."
                });

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = _authService.Login(request, ipAddress);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        /// <summary>
        /// Health check — verifies the token is valid.
        /// GET /api/auth/me
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Me()
        {
            var userID     = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
            var userName   = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var loginID    = User.FindFirst("unique_name")?.Value
                          ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value;
            var userTypeID = User.FindFirst("UserTypeID")?.Value;

            return Ok(new
            {
                UserID     = userID,
                UserName   = userName,
                UserLoginID= loginID,
                UserTypeID = userTypeID
            });
        }
    }
}
