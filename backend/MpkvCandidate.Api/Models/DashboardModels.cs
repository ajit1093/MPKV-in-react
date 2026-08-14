namespace MpkvCandidate.Api.Models
{
    // ── Candidate Dashboard ──────────────────────────────────────────────────

    public class CandidateDashboardResponse
    {
        public string ApplicationFormStatus { get; set; } = string.Empty;
        public string DocumentVerificationStatus { get; set; } = string.Empty;
        public bool IsFormLocked { get; set; }
        public ApplicationProgressResponse Progress { get; set; } = new();
        public List<RejectedDocumentDto> RejectedDocuments { get; set; } = new();
    }

    public class RejectedDocumentDto
    {
        public string Document { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    // ── Application Progress ─────────────────────────────────────────────────

    public class ApplicationProgressResponse
    {
        // Main round flags
        public bool Registration { get; set; } = true;
        public bool PersonalInfo { get; set; }
        public bool CollegeSelection { get; set; }
        public bool DocumentUpload { get; set; }
        public bool FeePayment { get; set; }
        public bool FormLocked { get; set; }
        public int TotalSteps { get; set; } = 6;
        public int CompletedSteps => 1
            + (PersonalInfo     ? 1 : 0)
            + (CollegeSelection ? 1 : 0)
            + (DocumentUpload   ? 1 : 0)
            + (FeePayment       ? 1 : 0)
            + (FormLocked       ? 1 : 0);
        public string NextStepUrl { get; set; } = string.Empty;

        // Round 2 sub-pages (Application Form — 5 pages)
        public bool PersonalDetails { get; set; }
        public bool AddressDetails { get; set; }
        public bool CategoryDetails { get; set; }
        public bool QualificationDetails { get; set; }
        public bool SportsDetails { get; set; }

        // Round 3 sub-pages (College Preference — 2 pages)
        public bool ShortlistOptions { get; set; }
        public bool SetPreferences { get; set; }

        // Round 4 sub-pages (Documents — 2 pages)
        public bool PhotoAndSign { get; set; }
        public bool RequiredDocuments { get; set; }
    }
}
