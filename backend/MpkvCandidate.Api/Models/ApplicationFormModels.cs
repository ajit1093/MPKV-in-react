namespace MpkvCandidate.Api.Models
{
    // ══════════════════════════════════════════════════════════════════════════
    // PERSONAL INFO  — mirrors PersonalEntity + Personal.aspx
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Response for GET /api/applicationform/masters/personal
    /// Returns courses + genders needed on the Personal page
    /// </summary>
    public class PersonalMastersResponse
    {
        public List<DropdownItem> Courses { get; set; } = new();
        public List<DropdownItem> Genders { get; set; } = new();
    }

    /// <summary>
    /// Response for GET /api/applicationform/personal
    /// Mirrors: PersonalWorker.GetPersonalDetails() → PersonalEntity
    /// </summary>
    public class PersonalDetailsResponse
    {
        public bool   Found          { get; set; }
        public long   CandidateID    { get; set; }
        public string ApplicationID  { get; set; } = string.Empty;
        public int    AppliedCourseID{ get; set; }
        public string CandidateName  { get; set; } = string.Empty;
        public string FatherName     { get; set; } = string.Empty;
        public string MotherName     { get; set; } = string.Empty;
        public string GenderCode     { get; set; } = string.Empty;
        /// <summary>yyyy-MM-dd — HTML date input format</summary>
        public string DOB            { get; set; } = string.Empty;
        /// <summary>Calculated as of July 1, 2026 — read-only display</summary>
        public string Age            { get; set; } = string.Empty;
        public string MobileNo       { get; set; } = string.Empty;
        public string EMailID        { get; set; } = string.Empty;
        /// <summary>1 = Yes, 0 = No  — mirrors IsResidentOfIndia radio</summary>
        public short  IsResidentOfIndia { get; set; } = 1;
    }

    /// <summary>
    /// Request for POST /api/applicationform/personal
    /// Mirrors: PersonalEntity fields used in SavePersonalDetails()
    /// </summary>
    public class SavePersonalRequest
    {
        public int    AppliedCourseID    { get; set; }
        public string CandidateName      { get; set; } = string.Empty;
        public string FatherName         { get; set; } = string.Empty;
        public string MotherName         { get; set; } = string.Empty;
        public string GenderCode         { get; set; } = string.Empty;
        /// <summary>Sent as "dd/MM/yyyy" from frontend to match old project format</summary>
        public string DOB                { get; set; } = string.Empty;
        public string MobileNo           { get; set; } = string.Empty;
        public string EMailID            { get; set; } = string.Empty;
        /// <summary>1 = Yes, 0 = No</summary>
        public short  IsResidentOfIndia  { get; set; } = 1;
    }

    public class SavePersonalResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ADDRESS  — mirrors AddressEntity + Address.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class AddressMastersResponse
    {
        public List<DropdownItem>        States    { get; set; } = new();
        /// <summary>All districts — frontend filters client-side by StateID (Group field)</summary>
        public List<DropdownItemGrouped> Districts { get; set; } = new();
    }

    public class DropdownItemGrouped
    {
        public string Value { get; set; } = string.Empty;
        public string Text  { get; set; } = string.Empty;
        /// <summary>Parent StateID — mirrors old DropdownEntity.Group</summary>
        public string Group { get; set; } = string.Empty;
    }

    public class AddressDetailsResponse
    {
        public bool   Found        { get; set; }
        // Permanent address
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public int    StateID      { get; set; } = 27;   // default Maharashtra
        public int    DistrictID   { get; set; }
        public string City         { get; set; } = string.Empty;
        public string Pincode      { get; set; } = string.Empty;
        // Correspondence address
        public bool   IsCorrAddressSameAsPermanent { get; set; }
        public string CorrAddressLine1 { get; set; } = string.Empty;
        public string CorrAddressLine2 { get; set; } = string.Empty;
        public int    CorrStateID      { get; set; } = 27;
        public int    CorrDistrictID   { get; set; }
        public string CorrCity         { get; set; } = string.Empty;
        public string CorrPincode      { get; set; } = string.Empty;
    }

    public class SaveAddressRequest
    {
        // Permanent
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public int    StateID      { get; set; }
        public int    DistrictID   { get; set; }
        public string City         { get; set; } = string.Empty;
        public string Pincode      { get; set; } = string.Empty;
        // Correspondence
        public bool   IsCorrAddressSameAsPermanent { get; set; }
        public string CorrAddressLine1 { get; set; } = string.Empty;
        public string CorrAddressLine2 { get; set; } = string.Empty;
        public int    CorrStateID      { get; set; }
        public int    CorrDistrictID   { get; set; }
        public string CorrCity         { get; set; } = string.Empty;
        public string CorrPincode      { get; set; } = string.Empty;
    }

    public class SaveAddressResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CATEGORY & OTHER RESERVATION — mirrors CategoryAndOtherReservationEntity
    // ══════════════════════════════════════════════════════════════════════════

    public class CategoryMastersResponse
    {
        /// <summary>Maharashtra districts only (Group == "27") — for DomicileDistrict dropdown</summary>
        public List<DropdownItem> DomicileDistricts { get; set; } = new();
        public List<DropdownItem> Categories        { get; set; } = new();
    }

    public class CategoryDetailsResponse
    {
        public bool   Found                    { get; set; }
        public int    DomicileDistrictID       { get; set; }
        public string DomicileVillage          { get; set; } = string.Empty;
        public int    CategoryID               { get; set; }
        public string Caste                    { get; set; } = string.Empty;
        public short  HasCasteCertificate      { get; set; }
        public short  HasReceiptCasteCertificate { get; set; }
        public short  HasNCLCertificate        { get; set; }
        public short  HasNCLReceipt            { get; set; }
        public short  HasEWSCertificate        { get; set; }
        // Other reservations
        public short  IsOrphan                 { get; set; }
        public short  IsPWD                    { get; set; }
        public short  IsExServiceman           { get; set; }
        public short  IsFreedomFighter         { get; set; }
        public short  IsProjectAffected        { get; set; }
        public short  IsNCC                    { get; set; }
        public short  IsSports                 { get; set; }
        public short  IsMPKVEmployee           { get; set; }
        public short  IsLandlessFarmLabourer   { get; set; }
        public short  IsIncomeSourceAgriculture{ get; set; }
        public short  HasFarm                  { get; set; }
    }

    public class SaveCategoryRequest
    {
        public int    DomicileDistrictID       { get; set; }
        public string DomicileVillage          { get; set; } = string.Empty;
        public int    CategoryID               { get; set; }
        public int    FinalCategoryID          { get; set; }
        public string Caste                    { get; set; } = string.Empty;
        public short  HasCasteCertificate      { get; set; }
        public short  HasReceiptCasteCertificate { get; set; }
        public short  HasNCLCertificate        { get; set; }
        public short  HasNCLReceipt            { get; set; }
        public short  HasEWSCertificate        { get; set; }
        public short  IsOrphan                 { get; set; }
        public short  IsPWD                    { get; set; }
        public short  IsExServiceman           { get; set; }
        public short  IsFreedomFighter         { get; set; }
        public short  IsProjectAffected        { get; set; }
        public short  IsNCC                    { get; set; }
        public short  IsSports                 { get; set; }
        public short  IsMPKVEmployee           { get; set; }
        public short  IsLandlessFarmLabourer   { get; set; }
        public short  IsIncomeSourceAgriculture{ get; set; }
        public short  HasFarm                  { get; set; }
    }

    public class SaveCategoryResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SPORTS DETAILS — mirrors SportsDetailsEntity + SportsDetails.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class SportsMastersResponse
    {
        /// <summary>Master_SportsCertificateType — CertificateTypeID / CertificateType ordered by SeqNo</summary>
        public List<DropdownItem> CertificateTypes { get; set; } = new();
    }

    public class SportsDetailsResponse
    {
        public bool  Found              { get; set; }
        /// <summary>true=Yes, false=No</summary>
        public bool  IsSportsCertificate { get; set; }
        public int   CertificateTypeID  { get; set; }
    }

    public class SaveSportsRequest
    {
        public bool  IsSportsCertificate { get; set; }
        /// <summary>0 when IsSportsCertificate=false — mirrors old CertificateTypeID=0 / DBNull logic</summary>
        public int   CertificateTypeID  { get; set; }
    }

    public class SaveSportsResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SHORTLIST OPTIONS — mirrors OptionFormEntity + ShortListOptions.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class CollegeOptionDto
    {
        public long   CollegeID   { get; set; }
        public string CollegeCode { get; set; } = string.Empty;
        public string CollegeName { get; set; } = string.Empty;
        public string District    { get; set; } = string.Empty;
        public string CourseStatus{ get; set; } = string.Empty;
        public int    PreferenceNo{ get; set; }
    }

    public class AvailableOptionsResponse
    {
        public List<CollegeOptionDto> Colleges { get; set; } = new();
    }

    public class ShortlistedOptionsResponse
    {
        public List<CollegeOptionDto> Colleges { get; set; } = new();
    }

    public class AddOptionRequest
    {
        public long CollegeID { get; set; }
    }

    public class RemoveOptionRequest
    {
        public long CollegeID { get; set; }
    }

    public class OptionActionResponse
    {
        public bool   Success     { get; set; }
        public string Message     { get; set; } = string.Empty;
        public string CollegeName { get; set; } = string.Empty;
    }

    public class SaveShortlistResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SET PREFERENCES — mirrors SetPreferences.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class PreferencedOptionsResponse
    {
        public List<CollegeOptionDto> Colleges { get; set; } = new();
    }

    public class PreferenceItem
    {
        public long CollegeID    { get; set; }
        public int  PreferenceNo { get; set; }
    }

    public class SavePreferencesRequest
    {
        /// <summary>All colleges with their assigned preference numbers — mirrors entity.ListOptions</summary>
        public List<PreferenceItem> Options { get; set; } = new();
    }

    public class SavePreferencesResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PHOTO & SIGNATURE — mirrors PhotoAndSignEntity + UploadPhotoAndSign.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class PhotoSignDetailsResponse
    {
        public bool   Found            { get; set; }
        public string PhotoUploadedURL { get; set; } = string.Empty;
        public string SignUploadedURL  { get; set; } = string.Empty;
        /// <summary>Both uploaded — mirrors old btnProceed.Enabled logic</summary>
        public bool   BothUploaded     { get; set; }
    }

    public class UploadPhotoSignResponse
    {
        public bool   Success     { get; set; }
        public string Message     { get; set; } = string.Empty;
        public string UploadedURL { get; set; } = string.Empty;
    }

    public class SavePhotoSignResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // QUALIFICATION — mirrors QualificationEntity + Qualification.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class QualificationMastersResponse
    {
        public List<DropdownItem> Qualifications  { get; set; } = new();
        /// <summary>Maharashtra districts only (Group=27) — same filter as old ddlPassingDistrict</summary>
        public List<DropdownItem> PassingDistricts{ get; set; } = new();
        /// <summary>Last 30 years from admission year 2026 — mirrors Helper.GetYearList(-30)</summary>
        public List<DropdownItem> PassingYears    { get; set; } = new();
        public List<DropdownItem> Boards          { get; set; } = new();
        /// <summary>1–22 — mirrors Helper.GetNumberList(22)</summary>
        public List<DropdownItem> EducationalGapYears { get; set; } = new();
        /// <summary>1–10 — mirrors Helper.GetNumberList(10)</summary>
        public List<DropdownItem> NoOfAttempts    { get; set; } = new();
    }

    public class QualificationDetailsResponse
    {
        public bool   Found                  { get; set; }
        /// <summary>Read-only label shown above the form — mirrors lblEligibilityQualificationHeader</summary>
        public string EligibilityQualification    { get; set; } = string.Empty;
        public short  EligibilityQualificationID  { get; set; }
        public short  HighestQualificationID  { get; set; }
        public short  IsEducationalGap        { get; set; }
        public short  EducationalGapYears     { get; set; }
        public string EducationalGapReason    { get; set; } = string.Empty;
        public string SeatNo                  { get; set; } = string.Empty;
        public short  NoOfAttempts            { get; set; }
        public int    PassingDistrictID       { get; set; }
        public short  PassingYear             { get; set; }
        public short  BoardID                 { get; set; }
        public int    MarksObtained           { get; set; }
        public int    MarksOutOf              { get; set; }
        public decimal Percentage             { get; set; }
    }

    public class SaveQualificationRequest
    {
        public short  HighestQualificationID  { get; set; }
        public short  IsEducationalGap        { get; set; }
        public short  EducationalGapYears     { get; set; }
        public string EducationalGapReason    { get; set; } = string.Empty;
        public short  EligibilityQualificationID { get; set; }
        public string SeatNo                  { get; set; } = string.Empty;
        public short  NoOfAttempts            { get; set; }
        public int    PassingDistrictID       { get; set; }
        public short  PassingYear             { get; set; }
        public short  BoardID                 { get; set; }
        public int    MarksObtained           { get; set; }
        public int    MarksOutOf              { get; set; }
    }

    public class SaveQualificationResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // REQUIRED DOCUMENTS — mirrors RequiredDocumentEntity + UploadRequiredDocuments.aspx
    // ══════════════════════════════════════════════════════════════════════════

    public class RequiredDocumentDto
    {
        public int    DocumentID           { get; set; }
        public string DocumentName         { get; set; } = string.Empty;
        /// <summary>1 = Compulsory, 0 = Optional — mirrors IsCompulsory</summary>
        public short  IsCompulsory         { get; set; }
        public string DocumentUploadedURL  { get; set; } = string.Empty;
        /// <summary>e.g. "pdf,jpg" — from DB per document</summary>
        public string FileTypesAllowed     { get; set; } = string.Empty;
        /// <summary>Max size in KB — from DB per document</summary>
        public int    MaxFileSizeAllowed   { get; set; }
        /// <summary>1 = all compulsory uploaded — mirrors IsAllCompulsoryDocumentsUploaded</summary>
        public short  IsAllCompulsoryDocumentsUploaded { get; set; }
        /// <summary>True = show DocumentNo + DocumentIssueDate fields in upload modal</summary>
        public bool   RequiresDocumentDetails { get; set; }
    }

    public class DocumentsListResponse
    {
        public List<RequiredDocumentDto> Documents       { get; set; } = new();
        public int                       TotalMandatory  { get; set; }
        public int                       UploadedMandatory { get; set; }
        public bool                      AllCompulsoryUploaded { get; set; }
    }

    public class UploadDocumentRequest
    {
        public int    DocumentID        { get; set; }
        public string DocumentNo        { get; set; } = string.Empty;
        public string DocumentIssueDate { get; set; } = string.Empty;   // dd/MM/yyyy
    }

    public class UploadDocumentResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UploadedURL { get; set; } = string.Empty;
    }

    public class DeleteDocumentRequest
    {
        public int DocumentID { get; set; }
    }

    public class DocumentActionResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SaveDocumentsResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FEE — mirrors ApplicationFeeEntity + PayApplicationFee.aspx
    // SP: ApplicationForm_GetApplicationFeeDetails
    // ══════════════════════════════════════════════════════════════════════════

    public class ApplicationFeeDto
    {
        public long    CandidateID      { get; set; }
        public string  ApplicationID    { get; set; } = string.Empty;
        public string  CandidateName    { get; set; } = string.Empty;
        public string  AppliedCourse    { get; set; } = string.Empty;
        public string  Gender           { get; set; } = string.Empty;
        public string  Category         { get; set; } = string.Empty;
        public string  IsPWD            { get; set; } = string.Empty;
        public int     FeeToBePaid      { get; set; }
        public int     FeeAlreadyPaid   { get; set; }
        public int     RemainingFee     { get; set; }
        public int     PhaseID          { get; set; }
        public string  Purpose          { get; set; } = string.Empty;
        /// <summary>List of payment gateway options from master table</summary>
        public List<PaymentGatewayOption> PaymentGateways { get; set; } = new();
    }

    public class PaymentGatewayOption
    {
        public int    PaymentGatewayID   { get; set; }
        public string PaymentGatewayName { get; set; } = string.Empty;
    }

    public class FeeDetailsResponse
    {
        public bool              Success { get; set; }
        public string            Message { get; set; } = string.Empty;
        public ApplicationFeeDto Fee     { get; set; } = new();
    }

    /// <summary>Request body for POST /api/applicationform/fee/initiate</summary>
    public class FeeInitiateRequest
    {
        public int PaymentGatewayID { get; set; }
    }

    /// <summary>Response from POST /api/applicationform/fee/initiate</summary>
    public class FeeInitiateResponse
    {
        public bool   Success            { get; set; }
        public string Message            { get; set; } = string.Empty;
        public long   TransactionID      { get; set; }
        public string PaymentGatewayURL  { get; set; } = string.Empty;
    }

    /// <summary>Response from POST /api/applicationform/fee/proceed  (fee = 0 path)</summary>
    public class FeeProceedResponse
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
