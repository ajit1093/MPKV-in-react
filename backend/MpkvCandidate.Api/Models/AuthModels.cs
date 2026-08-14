namespace MpkvCandidate.Api.Models
{
    // ── Request DTOs ─────────────────────────────────────────────────────────

    public class LoginRequest
    {
        public string UserLoginID { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public long UserID { get; set; }
        public string UserLoginID { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int UserTypeID { get; set; }
        public string DashBoardPath { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty;
        public int CourseID { get; set; }
        public int DistrictID { get; set; }
    }
}
