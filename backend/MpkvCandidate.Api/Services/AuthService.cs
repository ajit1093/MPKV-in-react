using Dapper;
using Microsoft.IdentityModel.Tokens;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MpkvCandidate.Api.Services
{
    public interface IAuthService
    {
        LoginResponse Login(LoginRequest request, string ipAddress);
    }

    public class AuthService : IAuthService
    {
        private readonly DbAccess _db;
        private readonly IConfiguration _config;

        public AuthService(DbAccess db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        public LoginResponse Login(LoginRequest request, string ipAddress)
        {
            try
            {
                // Password must be Base64 encoded before passing to SP —
                // same as CommonHelper.Base64Encrypt() in the old Web Forms project
                string encodedPassword = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(request.UserPassword));

                // Call the same SP the old Web Forms project uses
                var param = new DynamicParameters();
                param.Add("@UserLoginID",     request.UserLoginID);
                param.Add("@UserPassword",    encodedPassword);
                param.Add("@BrowserName",     "React");
                param.Add("@BrowserVersion",  "1.0");
                param.Add("@IPAddress",        ipAddress);

                var dt = _db.GetDataTable("Account_CheckUserExists", param);

                if (dt == null || dt.Rows.Count == 0)
                    return new LoginResponse { Success = false, Message = "Invalid login ID or password." };

                var row = dt.Rows[0];
                bool isAllowed = Convert.ToBoolean(row["IsLoginAllowed"]);

                if (!isAllowed)
                    return new LoginResponse
                    {
                        Success = false,
                        Message = row["ErrorMessage"]?.ToString() ?? "Login not allowed."
                    };

                // Only allow candidates (UserTypeID = 91)
                int userTypeID = Convert.ToInt32(row["UserTypeID"]);
                if (userTypeID != 91)
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "This portal is for candidates only."
                    };

                var user = new UserInfo
                {
                    UserID       = Convert.ToInt64(row["UserID"]),
                    UserLoginID  = row["UserLoginID"].ToString()!,
                    UserName     = row["UserName"].ToString()!,
                    UserTypeID   = userTypeID,
                    DashBoardPath= row["DashBoardPath"].ToString()!,
                    PhotoPath    = row["PhotoPath"].ToString()!,
                    CourseID     = Convert.ToInt32(row["CourseID"]),
                    DistrictID   = Convert.ToInt32(row["DistrictID"])
                };

                // Update login session in DB
                var sessionID = Convert.ToInt64(row["LoggedInSessionID"]);
                var sessionParam = new DynamicParameters();
                sessionParam.Add("@UserID",           user.UserID);
                sessionParam.Add("@LoggedInSessionID", sessionID);
                _db.ExecuteNonQuery("Account_UpdateLoginStatus", sessionParam);

                string token = GenerateJwtToken(user, sessionID);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token   = token,
                    User    = user
                };
            }
            catch (Exception ex)
            {
                // Return full error detail so we can diagnose
                return new LoginResponse { Success = false, Message = $"Login error: {ex.Message} | {ex.InnerException?.Message}" };
            }
        }

        private string GenerateJwtToken(UserInfo user, long sessionID)
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry  = DateTime.UtcNow.AddHours(Convert.ToInt32(_config["Jwt:ExpiryHours"] ?? "8"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,       user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,user.UserLoginID),
                new Claim(ClaimTypes.Name,                   user.UserName),
                new Claim("UserTypeID",                      user.UserTypeID.ToString()),
                new Claim("CourseID",                        user.CourseID.ToString()),
                new Claim("DistrictID",                      user.DistrictID.ToString()),
                new Claim("SessionID",                       sessionID.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,       Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             _config["Jwt:Issuer"],
                audience:           _config["Jwt:Audience"],
                claims:             claims,
                expires:            expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
