using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Models;
using MpkvCandidate.Api.Services;
using System.Security.Claims;

namespace MpkvCandidate.Api.Controllers
{
    [ApiController]
    [Route("api/applicationform")]
    [Authorize]                        // all application-form endpoints require a valid JWT
    public class ApplicationFormController : ControllerBase
    {
        private readonly IApplicationFormService _appFormService;

        public ApplicationFormController(IApplicationFormService appFormService)
        {
            _appFormService = appFormService;
        }

        // ── Helpers — read claims from JWT ────────────────────────────────────
        private long GetCandidateId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? "0";
            return long.TryParse(claim, out var id) ? id : 0;
        }

        private string GetUserLoginId()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("login")?.Value
                ?? string.Empty;
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — masters
        // GET /api/applicationform/masters/personal
        // Returns courses + genders dropdowns
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("masters/personal")]
        public IActionResult GetPersonalMasters()
        {
            var result = _appFormService.GetPersonalMasters();
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — load existing data
        // GET /api/applicationform/personal
        // Mirrors: Personal.aspx GetPersonal()
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("personal")]
        public IActionResult GetPersonal()
        {
            var candidateId  = GetCandidateId();
            var userLoginId  = GetUserLoginId();

            if (candidateId <= 0)
                return Unauthorized(new { message = "Invalid session. Please log in again." });

            var result = _appFormService.GetPersonalDetails(candidateId, userLoginId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — save
        // POST /api/applicationform/personal
        // Mirrors: Personal.aspx SavePersonal() → redirects to Summary on success
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("personal")]
        public IActionResult SavePersonal([FromBody] SavePersonalRequest request)
        {
            if (request == null)
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Invalid request." });

            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();

            if (candidateId <= 0)
                return Unauthorized(new { message = "Invalid session. Please log in again." });

            // Basic server-side validation — mirrors old ASP.NET validators
            if (request.AppliedCourseID <= 0)
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Please select the applied course." });
            if (string.IsNullOrWhiteSpace(request.CandidateName))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Candidate name is required." });
            if (string.IsNullOrWhiteSpace(request.FatherName))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Father's name is required." });
            if (string.IsNullOrWhiteSpace(request.MotherName))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Mother's name is required." });
            if (string.IsNullOrWhiteSpace(request.GenderCode))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Please select gender." });
            if (string.IsNullOrWhiteSpace(request.DOB))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Date of birth is required." });
            if (string.IsNullOrWhiteSpace(request.MobileNo))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Mobile number is required." });
            if (string.IsNullOrWhiteSpace(request.EMailID))
                return BadRequest(new SavePersonalResponse { Success = false, Message = "Email ID is required." });

            var result = _appFormService.SavePersonalDetails(
                candidateId, userLoginId, GetIpAddress(), request);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — masters (states + all districts)
        // GET /api/applicationform/masters/address
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("masters/address")]
        public IActionResult GetAddressMasters()
        {
            var result = _appFormService.GetAddressMasters();
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — load existing
        // GET /api/applicationform/address
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("address")]
        public IActionResult GetAddress()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0)
                return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetAddressDetails(candidateId, userLoginId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — save
        // POST /api/applicationform/address
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("address")]
        public IActionResult SaveAddress([FromBody] SaveAddressRequest request)
        {
            if (request == null)
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Invalid request." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0)
                return Unauthorized(new { message = "Invalid session." });
            // Server-side validation
            if (string.IsNullOrWhiteSpace(request.CorrAddressLine1))
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Correspondence Address Line 1 is required." });
            if (request.CorrStateID <= 0)
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Please select Correspondence State." });
            if (request.CorrDistrictID <= 0)
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Please select Correspondence District." });
            if (string.IsNullOrWhiteSpace(request.CorrCity))
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Correspondence City is required." });
            if (string.IsNullOrWhiteSpace(request.CorrPincode))
                return BadRequest(new SaveAddressResponse { Success = false, Message = "Correspondence Pincode is required." });
            if (!request.IsCorrAddressSameAsPermanent)
            {
                if (string.IsNullOrWhiteSpace(request.AddressLine1))
                    return BadRequest(new SaveAddressResponse { Success = false, Message = "Permanent Address Line 1 is required." });
                if (request.StateID <= 0)
                    return BadRequest(new SaveAddressResponse { Success = false, Message = "Please select Permanent State." });
                if (request.DistrictID <= 0)
                    return BadRequest(new SaveAddressResponse { Success = false, Message = "Please select Permanent District." });
                if (string.IsNullOrWhiteSpace(request.City))
                    return BadRequest(new SaveAddressResponse { Success = false, Message = "Permanent City is required." });
                if (string.IsNullOrWhiteSpace(request.Pincode))
                    return BadRequest(new SaveAddressResponse { Success = false, Message = "Permanent Pincode is required." });
            }
            var result = _appFormService.SaveAddressDetails(candidateId, userLoginId, GetIpAddress(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — masters (domicile districts + categories)
        // GET /api/applicationform/masters/category
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("masters/category")]
        public IActionResult GetCategoryMasters()
        {
            var result = _appFormService.GetCategoryMasters();
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — load existing
        // GET /api/applicationform/category
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("category")]
        public IActionResult GetCategory()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetCategoryDetails(candidateId, userLoginId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — save
        // POST /api/applicationform/category
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("category")]
        public IActionResult SaveCategory([FromBody] SaveCategoryRequest request)
        {
            if (request == null)
                return BadRequest(new SaveCategoryResponse { Success = false, Message = "Invalid request." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            if (request.DomicileDistrictID <= 0)
                return BadRequest(new SaveCategoryResponse { Success = false, Message = "Please select Domicile District." });
            if (request.CategoryID <= 0)
                return BadRequest(new SaveCategoryResponse { Success = false, Message = "Please select Category." });
            var result = _appFormService.SaveCategoryDetails(candidateId, userLoginId, GetIpAddress(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS — masters
        // GET /api/applicationform/masters/sports
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("masters/sports")]
        public IActionResult GetSportsMasters()
        {
            var result = _appFormService.GetSportsMasters();
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS — load existing
        // GET /api/applicationform/sports
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("sports")]
        public IActionResult GetSports()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetSportsDetails(candidateId, userLoginId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS — save
        // POST /api/applicationform/sports
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("sports")]
        public IActionResult SaveSports([FromBody] SaveSportsRequest request)
        {
            if (request == null)
                return BadRequest(new SaveSportsResponse { Success = false, Message = "Invalid request." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            if (request.IsSportsCertificate && request.CertificateTypeID <= 0)
                return BadRequest(new SaveSportsResponse { Success = false, Message = "Please Select Certificate Type." });
            var result = _appFormService.SaveSportsDetails(candidateId, userLoginId, GetIpAddress(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — available colleges
        // GET /api/applicationform/options/available
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("options/available")]
        public IActionResult GetAvailableOptions()
        {
            var candidateId = GetCandidateId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetAvailableOptions(candidateId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — shortlisted colleges
        // GET /api/applicationform/options/shortlisted
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("options/shortlisted")]
        public IActionResult GetShortlistedOptions()
        {
            var candidateId = GetCandidateId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetShortlistedOptions(candidateId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — add college
        // POST /api/applicationform/options/add
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("options/add")]
        public IActionResult AddOption([FromBody] AddOptionRequest request)
        {
            if (request == null || request.CollegeID <= 0)
                return BadRequest(new OptionActionResponse { Success = false, Message = "Invalid request." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.AddOption(candidateId, userLoginId, GetIpAddress(), request.CollegeID);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — remove college
        // DELETE /api/applicationform/options/remove
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("options/remove/{collegeId}")]
        public IActionResult RemoveOption(long collegeId)
        {
            if (collegeId <= 0) return BadRequest(new OptionActionResponse { Success = false, Message = "Invalid CollegeID." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.RemoveOption(candidateId, userLoginId, GetIpAddress(), collegeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — save final order (Proceed)
        // POST /api/applicationform/options/save
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("options/save")]
        public IActionResult SaveShortlist()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.SaveShortlist(candidateId, userLoginId, GetIpAddress());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SET PREFERENCES — load colleges with current preference numbers
        // GET /api/applicationform/options/preferenced
        // Mirrors: SetPreferences.aspx GetShortlistedOptions()
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("options/preferenced")]
        public IActionResult GetPreferencedOptions()
        {
            var candidateId = GetCandidateId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetPreferencedOptions(candidateId);
            // If empty — redirect to shortlist (mirrors old Response.Redirect("ShortListOptions.aspx"))
            if (result.Colleges.Count == 0)
                return Ok(new PreferencedOptionsResponse());
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SET PREFERENCES — save preferences
        // POST /api/applicationform/options/preferences
        // Mirrors: SetPreferences.aspx btnProceed_Click
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("options/preferences")]
        public IActionResult SavePreferences([FromBody] SavePreferencesRequest request)
        {
            if (request == null)
                return BadRequest(new SavePreferencesResponse { Success = false, Message = "Invalid request." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.SavePreferences(candidateId, userLoginId, GetIpAddress(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PREFERENCES — reset (set all to 0 in DB)
        // POST /api/applicationform/options/preferences/reset
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("options/preferences/reset")]
        public IActionResult ResetPreferences()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.ResetPreferences(candidateId, userLoginId, GetIpAddress());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PHOTO & SIGN — get existing URLs
        // GET /api/applicationform/photo-sign
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("photo-sign")]
        public IActionResult GetPhotoSign()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.GetPhotoSignDetails(candidateId, userLoginId);
            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PHOTO — upload
        // POST /api/applicationform/upload-photo
        // Mirrors: btnUploadPhoto_Click — JPG/JPEG only, 10KB–100KB
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("upload-photo")]
        [RequestSizeLimit(200 * 1024)]   // 200KB max
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new UploadPhotoSignResponse { Success = false, Message = "Please Select Photograph to Upload." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = await _appFormService.UploadPhoto(candidateId, userLoginId, GetIpAddress(), file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SIGNATURE — upload
        // POST /api/applicationform/upload-sign
        // Mirrors: btnUploadSign_Click — JPG/JPEG only, 5KB–50KB
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("upload-sign")]
        [RequestSizeLimit(100 * 1024)]   // 100KB max
        public async Task<IActionResult> UploadSign([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new UploadPhotoSignResponse { Success = false, Message = "Please Select Signature to Upload." });
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = await _appFormService.UploadSign(candidateId, userLoginId, GetIpAddress(), file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PHOTO & SIGN — mark step complete (Proceed)
        // POST /api/applicationform/photo-sign/save
        // Mirrors: btnProceed_Click → SavePhotoAndSign()
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("photo-sign/save")]
        public IActionResult SavePhotoSign()
        {
            var candidateId = GetCandidateId();
            var userLoginId = GetUserLoginId();
            if (candidateId <= 0) return Unauthorized(new { message = "Invalid session." });
            var result = _appFormService.SavePhotoSign(candidateId, userLoginId, GetIpAddress());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // QUALIFICATION — masters
        [HttpGet("masters/qualification")]
        public IActionResult GetQualificationMasters()
            => Ok(_appFormService.GetQualificationMasters());

        // QUALIFICATION — load existing
        [HttpGet("qualification")]
        public IActionResult GetQualification()
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            return Ok(_appFormService.GetQualificationDetails(c, GetUserLoginId()));
        }

        // QUALIFICATION — save
        [HttpPost("qualification")]
        public IActionResult SaveQualification([FromBody] SaveQualificationRequest request)
        {
            if (request == null) return BadRequest(new SaveQualificationResponse { Success=false, Message="Invalid request." });
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            if (request.HighestQualificationID <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Select Highest Qualification." });
            if (string.IsNullOrWhiteSpace(request.SeatNo)) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Enter Seat No." });
            if (request.PassingDistrictID <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Select Passing District." });
            if (request.PassingYear <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Select Passing Year." });
            if (request.BoardID <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Select Board." });
            if (request.MarksObtained <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Enter Marks Obtained." });
            if (request.MarksOutOf <= 0) return BadRequest(new SaveQualificationResponse { Success=false, Message="Please Enter Marks Out Of." });
            var result = _appFormService.SaveQualificationDetails(c, GetUserLoginId(), GetIpAddress(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOCUMENTS — list
        // GET /api/applicationform/documents
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("documents")]
        public IActionResult GetDocuments()
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            return Ok(_appFormService.GetDocumentsList(c, GetUserLoginId()));
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOCUMENTS — upload
        // POST /api/applicationform/documents/upload  (multipart/form-data)
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("documents/upload")]
        [RequestSizeLimit(10 * 1024 * 1024)]   // 10MB max
        public async Task<IActionResult> UploadDocument(
            [FromForm] int documentId,
            [FromForm] string? documentNo,
            [FromForm] string? documentIssueDate,
            [FromForm] IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new UploadDocumentResponse { Success = false, Message = "Please select a file to upload." });
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            var request = new UploadDocumentRequest
            {
                DocumentID        = documentId,
                DocumentNo        = documentNo        ?? "",
                DocumentIssueDate = documentIssueDate ?? "",
            };
            var result = await _appFormService.UploadDocument(c, GetUserLoginId(), GetIpAddress(), request, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOCUMENTS — delete
        // DELETE /api/applicationform/documents/delete/{documentId}
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("documents/delete/{documentId}")]
        public IActionResult DeleteDocument(int documentId)
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            var result = _appFormService.DeleteDocument(c, GetUserLoginId(), GetIpAddress(), documentId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOCUMENTS — save (proceed)
        // POST /api/applicationform/documents/save
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("documents/save")]
        public IActionResult SaveDocuments()
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            var result = _appFormService.SaveDocuments(c, GetUserLoginId(), GetIpAddress());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FEE — load details
        // GET /api/applicationform/fee
        // Mirrors: Page_Load → GetApplicationFee() + CheckFailedTransactions()
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("fee")]
        public IActionResult GetFee()
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            var result = _appFormService.GetFeeDetails(c, GetUserLoginId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FEE — initiate gateway transaction (fee > 0)
        // POST /api/applicationform/fee/initiate
        // Mirrors: btnPay_Click → FeeWorker.SetFeeTransaction()
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("fee/initiate")]
        public IActionResult InitiateFee([FromBody] FeeInitiateRequest request)
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            if (request == null || request.PaymentGatewayID <= 0)
                return BadRequest(new FeeInitiateResponse { Success = false, Message = "Please select a payment gateway." });
            var result = _appFormService.InitiateFeeTransaction(c, GetUserLoginId(), GetIpAddress(), request.PaymentGatewayID);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FEE — proceed without payment (fee = 0 or already paid)
        // POST /api/applicationform/fee/proceed
        // Mirrors: btnProceed_Click → SaveApplicationFeeDetails()
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("fee/proceed")]
        public IActionResult ProceedFee()
        {
            var c = GetCandidateId(); if (c <= 0) return Unauthorized();
            var result = _appFormService.SaveFeeDetails(c, GetUserLoginId(), GetIpAddress());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
