namespace MpkvCandidate.Api.Models
{
    // ── Request DTOs ─────────────────────────────────────────────────────────

    public class RegisterRequest
    {
        public int    AppliedCourseID          { get; set; }
        public string CandidateName            { get; set; } = string.Empty;
        public string FatherName               { get; set; } = string.Empty;
        public string MotherName               { get; set; } = string.Empty;
        public string GenderCode               { get; set; } = string.Empty;
        public string DOB                      { get; set; } = string.Empty;   // "dd/MM/yyyy" from frontend
        public string MobileNo                 { get; set; } = string.Empty;
        public string EMailID                  { get; set; } = string.Empty;
        public short  SecurityQuestionID       { get; set; }
        public string SecurityQuestionAnswer   { get; set; } = string.Empty;
        public string Password                 { get; set; } = string.Empty;
    }

    // ── Response DTOs ────────────────────────────────────────────────────────

    public class RegisterResponse
    {
        public bool   Success       { get; set; }
        public string Message       { get; set; } = string.Empty;
        public string? LoginID      { get; set; }   // 10-digit Application ID on success
        public string? CandidateName { get; set; }
    }

    public class RegistrationMastersResponse
    {
        public List<DropdownItem> Courses           { get; set; } = new();
        public List<DropdownItem> Genders           { get; set; } = new();
        public List<DropdownItem> SecurityQuestions { get; set; } = new();
    }

    public class RegistrationStatusResponse
    {
        public bool IsOpen { get; set; }
    }

    public class RegistrationInfoResponse
    {
        public bool   Found         { get; set; }
        public string LoginID       { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
    }

    // ── Shared dropdown ──────────────────────────────────────────────────────
    public class DropdownItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text  { get; set; } = string.Empty;
    }
}
