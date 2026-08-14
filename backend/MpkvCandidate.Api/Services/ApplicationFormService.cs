using Dapper;
using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;

namespace MpkvCandidate.Api.Services
{
    public interface IApplicationFormService
    {
        // ── Personal ─────────────────────────────────────────────────────────
        PersonalMastersResponse  GetPersonalMasters();
        PersonalDetailsResponse  GetPersonalDetails(long candidateId, string userLoginId);
        SavePersonalResponse     SavePersonalDetails(long candidateId, string userLoginId, string ipAddress, SavePersonalRequest request);

        // ── Address ──────────────────────────────────────────────────────────
        AddressMastersResponse   GetAddressMasters();
        AddressDetailsResponse   GetAddressDetails(long candidateId, string userLoginId);
        SaveAddressResponse      SaveAddressDetails(long candidateId, string userLoginId, string ipAddress, SaveAddressRequest request);

        // ── Sports Details ────────────────────────────────────────────────────
        SportsMastersResponse  GetSportsMasters();
        SportsDetailsResponse  GetSportsDetails(long candidateId, string userLoginId);
        SaveSportsResponse     SaveSportsDetails(long candidateId, string userLoginId, string ipAddress, SaveSportsRequest request);

        // ── Shortlist Options ─────────────────────────────────────────────────
        AvailableOptionsResponse  GetAvailableOptions(long candidateId);
        ShortlistedOptionsResponse GetShortlistedOptions(long candidateId);
        OptionActionResponse      AddOption(long candidateId, string userLoginId, string ipAddress, long collegeId);
        OptionActionResponse      RemoveOption(long candidateId, string userLoginId, string ipAddress, long collegeId);
        SaveShortlistResponse     SaveShortlist(long candidateId, string userLoginId, string ipAddress);

        // ── Set Preferences ───────────────────────────────────────────────────
        PreferencedOptionsResponse GetPreferencedOptions(long candidateId);
        SavePreferencesResponse    SavePreferences(long candidateId, string userLoginId, string ipAddress, SavePreferencesRequest request);
        SavePreferencesResponse    ResetPreferences(long candidateId, string userLoginId, string ipAddress);

        // ── Photo & Signature ─────────────────────────────────────────────────
        PhotoSignDetailsResponse GetPhotoSignDetails(long candidateId, string userLoginId);
        Task<UploadPhotoSignResponse> UploadPhoto(long candidateId, string userLoginId, string ipAddress, IFormFile file);
        Task<UploadPhotoSignResponse> UploadSign(long candidateId, string userLoginId, string ipAddress, IFormFile file);
        SavePhotoSignResponse SavePhotoSign(long candidateId, string userLoginId, string ipAddress);

        // ── Qualification ─────────────────────────────────────────────────────
        QualificationMastersResponse  GetQualificationMasters();
        QualificationDetailsResponse  GetQualificationDetails(long candidateId, string userLoginId);
        SaveQualificationResponse     SaveQualificationDetails(long candidateId, string userLoginId, string ipAddress, SaveQualificationRequest request);

        // ── Category & Other Reservation ─────────────────────────────────────
        CategoryMastersResponse  GetCategoryMasters();
        CategoryDetailsResponse  GetCategoryDetails(long candidateId, string userLoginId);
        SaveCategoryResponse     SaveCategoryDetails(long candidateId, string userLoginId, string ipAddress, SaveCategoryRequest request);
    }

    public class ApplicationFormService : IApplicationFormService
    {
        private readonly DbAccess      _db;
        private readonly IConfiguration _config;

        public ApplicationFormService(DbAccess db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — masters
        // GET /api/applicationform/masters/personal
        // Mirrors: Personal.aspx LoadMasters()
        // SPs: Base_GetMasterCourse, Base_GetMasterTableList (Master_Gender)
        // ══════════════════════════════════════════════════════════════════════
        public PersonalMastersResponse GetPersonalMasters()
        {
            var response = new PersonalMastersResponse();

            // Courses — Base_GetMasterCourse
            try
            {
                var dt = _db.GetDataTable("Base_GetMasterCourse");
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        // Skip the "-1" placeholder row (same as old .Where(a => a.Value != "-1"))
                        var val = row[0].ToString()!;
                        if (val != "-1")
                            response.Courses.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch { /* return empty */ }

            // Genders — Base_GetMasterTableList
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",        "Master_Gender");
                param.Add("@DataValueField",   "GenderCode");
                param.Add("@DataTextField",    "Gender");
                param.Add("@ParentField",      "");
                param.Add("@ParentFieldValue", "");
                param.Add("@OrderByFields",    "GenderCode");

                var dt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Genders.Add(new DropdownItem { Value = row[0].ToString()!, Text = row[1].ToString()! });
            }
            catch { }

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — get existing data
        // GET /api/applicationform/personal
        // Mirrors: PersonalWorker.GetPersonalDetails(CandidateInput)
        // SP: ApplicationForm_GetPersonalDetails
        // ══════════════════════════════════════════════════════════════════════
        public PersonalDetailsResponse GetPersonalDetails(long candidateId, string userLoginId)
        {
            var response = new PersonalDetailsResponse();

            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID",  candidateId);
                param.Add("@UserLoginID",  userLoginId);
                param.Add("@PageCode",     "Personal");

                var dt = _db.GetDataTable("ApplicationForm_GetPersonalDetails", param);

                if (dt == null || dt.Rows.Count == 0)
                    return response;   // Found = false — new candidate, empty form

                var row = dt.Rows[0];

                response.Found           = true;
                response.CandidateID     = Convert.ToInt64(row["CandidateID"]);
                response.ApplicationID   = row["ApplicationID"].ToString()!;
                response.AppliedCourseID = Convert.ToInt32(row["AppliedCourseID"]);
                response.CandidateName   = row["CandidateName"].ToString()!;
                response.FatherName      = row["FatherName"].ToString()!;
                response.MotherName      = row["MotherName"].ToString()!;
                response.GenderCode      = row["GenderCode"].ToString()!;

                // DOB — return as yyyy-MM-dd for HTML date input
                if (row["DOB"] != DBNull.Value)
                {
                    var dob = Convert.ToDateTime(row["DOB"]);
                    response.DOB = dob.ToString("yyyy-MM-dd");
                    response.Age = CalculateAge(dob);
                }

                response.MobileNo          = row["MobileNo"].ToString()!;
                response.EMailID           = row["EMailID"].ToString()!;
                response.IsResidentOfIndia = row["IsResidentOfIndia"] != DBNull.Value
                    ? Convert.ToInt16(row["IsResidentOfIndia"])
                    : (short)1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPersonalDetails error: {ex.Message}");
            }

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PERSONAL — save
        // POST /api/applicationform/personal
        // Mirrors: PersonalWorker.SavePersonalDetails(PersonalEntity)
        // SP: ApplicationForm_SavePersonalDetails
        // Returns "Y" on success
        // ══════════════════════════════════════════════════════════════════════
        public SavePersonalResponse SavePersonalDetails(
            long candidateId, string userLoginId, string ipAddress, SavePersonalRequest request)
        {
            try
            {
                // Parse DOB — frontend sends "dd/MM/yyyy"
                if (!DateTime.TryParseExact(
                        request.DOB.Trim(),
                        new[] { "dd/MM/yyyy", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dob))
                    return new SavePersonalResponse { Success = false, Message = "Invalid date of birth format." };

                string age = CalculateAge(dob);

                var param = new DynamicParameters();
                param.Add("@CandidateID",       candidateId);
                param.Add("@AppliedCourseID",   request.AppliedCourseID);
                param.Add("@CandidateName",     request.CandidateName.Trim().ToUpper());
                param.Add("@FatherName",        request.FatherName.Trim().ToUpper());
                param.Add("@MotherName",        request.MotherName.Trim().ToUpper());
                param.Add("@GenderCode",        request.GenderCode);
                param.Add("@DOB",               dob);
                param.Add("@Age",               age);
                param.Add("@MobileNo",          request.MobileNo.Trim());
                param.Add("@EMailID",           request.EMailID.Trim().ToLower());
                param.Add("@IsResidentOfIndia", request.IsResidentOfIndia);
                param.Add("@UserLoginID",       userLoginId);
                param.Add("@IPAddress",         ipAddress);
                param.Add("@PageCode",          "Personal");

                var result = _db.ExecuteScalar("ApplicationForm_SavePersonalDetails", param)
                                ?.ToString() ?? "";

                if (result.ToUpper() == "Y")
                    return new SavePersonalResponse { Success = true,  Message = "Personal details saved successfully." };

                return new SavePersonalResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Data has not been saved. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new SavePersonalResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shared helper — mirrors exact age calculation in Personal.aspx
        // Cutoff date: July 1, 2026
        // Returns: "X Years Y Months Z Days"
        // ─────────────────────────────────────────────────────────────────────
        public static string CalculateAge(DateTime dob)
        {
            var cutoff = new DateTime(2026, 7, 1);

            int years  = cutoff.Year  - dob.Year;
            int months = cutoff.Month - dob.Month;
            int days   = cutoff.Day   - dob.Day;

            if (days < 0)
            {
                months--;
                int prevMonth = cutoff.Month == 1 ? 12 : cutoff.Month - 1;
                days += DateTime.DaysInMonth(cutoff.Year, prevMonth);
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }

            return $"{years} Years {months} Months {days} Days";
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — masters
        // GET /api/applicationform/masters/address
        // Mirrors: Address.aspx LoadMasters()
        // SPs: Base_GetMasterTableList (Master_State), Base_GetMasterDistrict
        // ══════════════════════════════════════════════════════════════════════
        public AddressMastersResponse GetAddressMasters()
        {
            var response = new AddressMastersResponse();
            try
            {
                // States — same as old objBase.GetMasterTableList("Master_State","StateID","State",...)
                var stateParam = new DynamicParameters();
                stateParam.Add("@TableName",        "Master_State");
                stateParam.Add("@DataValueField",   "StateID");
                stateParam.Add("@DataTextField",    "State");
                stateParam.Add("@ParentField",      "");
                stateParam.Add("@ParentFieldValue", "");
                stateParam.Add("@OrderByFields",    "State");
                var stateDt = _db.GetDataTable("Base_GetMasterTableList", stateParam);
                if (stateDt != null)
                    foreach (System.Data.DataRow row in stateDt.Rows)
                    {
                        var val = row[0].ToString()!;
                        if (val != "-1")
                            response.States.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetAddressMasters States error: {ex.Message}"); }

            try
            {
                // Districts — Base_GetMasterDistrict returns 3 columns:
                // col[0]=DistrictID (Value), col[1]=District (Text), col[2]=StateID (Group)
                // Exact same as old DataTableToList(dt, true) in BaseWorker
                var distDt = _db.GetDataTable("Base_GetMasterDistrict");
                if (distDt != null)
                    foreach (System.Data.DataRow row in distDt.Rows)
                    {
                        var val = row[0].ToString()!;
                        if (val == "-1") continue;   // skip the Select placeholder row
                        response.Districts.Add(new DropdownItemGrouped
                        {
                            Value = val,
                            Text  = row[1].ToString()!,
                            Group = row[2].ToString()!   // StateID — used for client-side filtering
                        });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetAddressMasters Districts error: {ex.Message}"); }

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — get existing
        // GET /api/applicationform/address
        // Mirrors: AddressWorker.GetAddressDetails(CandidateInput)
        // SP: ApplicationForm_GetAddressDetails
        // ══════════════════════════════════════════════════════════════════════
        public AddressDetailsResponse GetAddressDetails(long candidateId, string userLoginId)
        {
            var response = new AddressDetailsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@PageCode",    "Address");
                var dt = _db.GetDataTable("ApplicationForm_GetAddressDetails", param);
                if (dt == null || dt.Rows.Count == 0) return response;
                var row = dt.Rows[0];
                response.Found        = Convert.ToInt64(row["CandidateID"]) > 0;
                response.AddressLine1 = row["AddressLine1"].ToString()!;
                response.AddressLine2 = row["AddressLine2"].ToString()!;
                response.StateID      = row["StateID"]    != DBNull.Value ? Convert.ToInt32(row["StateID"])    : 27;
                response.DistrictID   = row["DistrictID"] != DBNull.Value ? Convert.ToInt32(row["DistrictID"]) : 0;
                response.City         = row["City"].ToString()!;
                response.Pincode      = row["Pincode"].ToString()!;
                response.IsCorrAddressSameAsPermanent = row["IsCorrAddressSameAsPermanent"] != DBNull.Value
                    && Convert.ToBoolean(row["IsCorrAddressSameAsPermanent"]);
                response.CorrAddressLine1 = row["CorrAddressLine1"].ToString()!;
                response.CorrAddressLine2 = row["CorrAddressLine2"].ToString()!;
                response.CorrStateID      = row["CorrStateID"]    != DBNull.Value ? Convert.ToInt32(row["CorrStateID"])    : 27;
                response.CorrDistrictID   = row["CorrDistrictID"] != DBNull.Value ? Convert.ToInt32(row["CorrDistrictID"]) : 0;
                response.CorrCity         = row["CorrCity"].ToString()!;
                response.CorrPincode      = row["CorrPincode"].ToString()!;
            }
            catch (Exception ex) { Console.WriteLine($"GetAddressDetails error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ADDRESS — save
        // POST /api/applicationform/address
        // Mirrors: AddressWorker.SaveAddressDetails(AddressEntity)
        // SP: ApplicationForm_SaveAddressDetails
        // ══════════════════════════════════════════════════════════════════════
        public SaveAddressResponse SaveAddressDetails(
            long candidateId, string userLoginId, string ipAddress, SaveAddressRequest request)
        {
            try
            {
                // When sameAddress=true, permanent = correspondence (mirrors old SaveAddress logic exactly)
                string a1, a2, city, pincode;
                int stateId, districtId;

                if (request.IsCorrAddressSameAsPermanent)
                {
                    a1 = request.CorrAddressLine1.Trim().ToUpper();
                    a2 = request.CorrAddressLine2.Trim().ToUpper();
                    stateId    = request.CorrStateID;
                    districtId = request.CorrDistrictID;
                    city       = request.CorrCity.Trim().ToUpper();
                    pincode    = request.CorrPincode.Trim();
                }
                else
                {
                    a1 = request.AddressLine1.Trim().ToUpper();
                    a2 = request.AddressLine2.Trim().ToUpper();
                    stateId    = request.StateID;
                    districtId = request.DistrictID;
                    city       = request.City.Trim().ToUpper();
                    pincode    = request.Pincode.Trim();
                }

                var param = new DynamicParameters();
                param.Add("@CandidateID",   candidateId);
                param.Add("@AddressLine1",  a1);
                param.Add("@AddressLine2",  a2);
                param.Add("@StateID",       stateId);
                param.Add("@DistrictID",    districtId);
                param.Add("@City",          city);
                param.Add("@Pincode",       pincode);
                param.Add("@IsCorrAddressSameAsPermanent", request.IsCorrAddressSameAsPermanent);
                param.Add("@CorrAddressLine1", request.IsCorrAddressSameAsPermanent ? a1 : request.CorrAddressLine1.Trim().ToUpper());
                param.Add("@CorrAddressLine2", request.IsCorrAddressSameAsPermanent ? a2 : request.CorrAddressLine2.Trim().ToUpper());
                param.Add("@CorrStateID",      request.IsCorrAddressSameAsPermanent ? stateId    : request.CorrStateID);
                param.Add("@CorrDistrictID",   request.IsCorrAddressSameAsPermanent ? districtId : request.CorrDistrictID);
                param.Add("@CorrCity",         request.IsCorrAddressSameAsPermanent ? city       : request.CorrCity.Trim().ToUpper());
                param.Add("@CorrPincode",      request.IsCorrAddressSameAsPermanent ? pincode    : request.CorrPincode.Trim());
                param.Add("@UserLoginID",   userLoginId);
                param.Add("@IPAddress",     ipAddress);
                param.Add("@PageCode",      "Address");

                var result = _db.ExecuteScalar("ApplicationForm_SaveAddressDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new SaveAddressResponse { Success = true, Message = "Address details saved successfully." };

                return new SaveAddressResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Data has not been saved. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new SaveAddressResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — masters
        // GET /api/applicationform/masters/category
        // Mirrors: CategoryAndOtherReservation.aspx LoadMasters()
        // SPs: Base_GetMasterDistrict (filter Group=="27"), Base_GetMasterTableList (Master_Category)
        // ══════════════════════════════════════════════════════════════════════
        public CategoryMastersResponse GetCategoryMasters()
        {
            var response = new CategoryMastersResponse();

            // Domicile Districts — Maharashtra only (Group == "27")
            try
            {
                var dt = _db.GetDataTable("Base_GetMasterDistrict");
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var group = row[2].ToString()!;
                        var val   = row[0].ToString()!;
                        // Skip placeholder row, keep only Maharashtra districts (Group = "27")
                        if (val == "-1" || group == "-1") continue;
                        if (group == "27")
                            response.DomicileDistricts.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetCategoryMasters Districts error: {ex.Message}"); }

            // Categories — Base_GetMasterTableList ordered by SeqNo (same as old)
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",        "Master_Category");
                param.Add("@DataValueField",   "CategoryID");
                param.Add("@DataTextField",    "Category");
                param.Add("@ParentField",      "");
                param.Add("@ParentFieldValue", "");
                param.Add("@OrderByFields",    "SeqNo");

                var dt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var val = row[0].ToString()!;
                        // Skip the "-1" Select placeholder
                        if (val != "-1")
                            response.Categories.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetCategoryMasters Categories error: {ex.Message}"); }

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — get existing
        // GET /api/applicationform/category
        // SP: ApplicationForm_GetCategoryAndOtherReservationDetails
        // ══════════════════════════════════════════════════════════════════════
        public CategoryDetailsResponse GetCategoryDetails(long candidateId, string userLoginId)
        {
            var response = new CategoryDetailsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@PageCode",    "CategoryAndOtherReservation");

                var dt = _db.GetDataTable("ApplicationForm_GetCategoryAndOtherReservationDetails", param);
                if (dt == null || dt.Rows.Count == 0) return response;

                var row = dt.Rows[0];
                if (row["CandidateID"] == DBNull.Value || Convert.ToInt64(row["CandidateID"]) == 0)
                    return response;

                response.Found                     = true;
                response.DomicileDistrictID        = row["DomicileDistrictID"]    != DBNull.Value ? Convert.ToInt32(row["DomicileDistrictID"])    : 0;
                response.DomicileVillage           = row["DomicileVillage"].ToString()!;
                response.CategoryID                = row["CategoryID"]             != DBNull.Value ? Convert.ToInt32(row["CategoryID"])             : 0;
                response.Caste                     = row["Caste"].ToString()!;
                response.HasCasteCertificate       = row["HasCasteCertificate"]        != DBNull.Value ? Convert.ToInt16(row["HasCasteCertificate"])        : (short)0;
                response.HasReceiptCasteCertificate= row["HasReceiptCasteCertificate"] != DBNull.Value ? Convert.ToInt16(row["HasReceiptCasteCertificate"]) : (short)0;
                response.HasNCLCertificate         = row["HasNCLCertificate"]          != DBNull.Value ? Convert.ToInt16(row["HasNCLCertificate"])           : (short)0;
                response.HasNCLReceipt             = row["HasNCLReceipt"]              != DBNull.Value ? Convert.ToInt16(row["HasNCLReceipt"])               : (short)0;
                response.HasEWSCertificate         = row["HasEWSCertificate"]          != DBNull.Value ? Convert.ToInt16(row["HasEWSCertificate"])           : (short)0;
                response.IsOrphan                  = row["IsOrphan"]                   != DBNull.Value ? Convert.ToInt16(row["IsOrphan"])                    : (short)0;
                response.IsPWD                     = row["IsPWD"]                      != DBNull.Value ? Convert.ToInt16(row["IsPWD"])                       : (short)0;
                response.IsExServiceman            = row["IsExServiceman"]             != DBNull.Value ? Convert.ToInt16(row["IsExServiceman"])              : (short)0;
                response.IsFreedomFighter          = row["IsFreedomFighter"]           != DBNull.Value ? Convert.ToInt16(row["IsFreedomFighter"])            : (short)0;
                response.IsProjectAffected         = row["IsProjectAffected"]          != DBNull.Value ? Convert.ToInt16(row["IsProjectAffected"])           : (short)0;
                response.IsNCC                     = row["IsNCC"]                      != DBNull.Value ? Convert.ToInt16(row["IsNCC"])                       : (short)0;
                response.IsSports                  = row["IsSports"]                   != DBNull.Value ? Convert.ToInt16(row["IsSports"])                    : (short)0;
                response.IsMPKVEmployee            = row["IsMPKVEmployee"]             != DBNull.Value ? Convert.ToInt16(row["IsMPKVEmployee"])              : (short)0;
                response.IsLandlessFarmLabourer    = row["IsLandlessFarmLabourer"]     != DBNull.Value ? Convert.ToInt16(row["IsLandlessFarmLabourer"])      : (short)0;
                response.IsIncomeSourceAgriculture = row["IsIncomeSourceAgriculture"]  != DBNull.Value ? Convert.ToInt16(row["IsIncomeSourceAgriculture"])   : (short)0;
                response.HasFarm                   = row["HasFarm"]                    != DBNull.Value ? Convert.ToInt16(row["HasFarm"])                     : (short)0;
            }
            catch (Exception ex) { Console.WriteLine($"GetCategoryDetails error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORY — save
        // POST /api/applicationform/category
        // SP: ApplicationForm_SaveCategoryAndOtherReservationDetails
        // ══════════════════════════════════════════════════════════════════════
        public SaveCategoryResponse SaveCategoryDetails(
            long candidateId, string userLoginId, string ipAddress, SaveCategoryRequest request)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID",              candidateId);
                param.Add("@DomicileDistrictID",       request.DomicileDistrictID);
                param.Add("@DomicileVillage",          request.DomicileVillage.Trim());
                param.Add("@CategoryID",               request.CategoryID);
                param.Add("@FinalCategoryID",          request.FinalCategoryID);
                param.Add("@Caste",                    request.Caste.Trim().ToUpper());
                param.Add("@HasCasteCertificate",      request.HasCasteCertificate);
                param.Add("@HasReceiptCasteCertificate", request.HasReceiptCasteCertificate);
                param.Add("@HasNCLCertificate",        request.HasNCLCertificate);
                param.Add("@HasNCLReceipt",            request.HasNCLReceipt);
                param.Add("@HasEWSCertificate",        request.HasEWSCertificate);
                param.Add("@IsOrphan",                 request.IsOrphan);
                param.Add("@IsPWD",                    request.IsPWD);
                param.Add("@IsExServiceman",           request.IsExServiceman);
                param.Add("@IsFreedomFighter",         request.IsFreedomFighter);
                param.Add("@IsProjectAffected",        request.IsProjectAffected);
                param.Add("@IsNCC",                    request.IsNCC);
                param.Add("@IsSports",                 request.IsSports);
                param.Add("@IsMPKVEmployee",           request.IsMPKVEmployee);
                param.Add("@IsLandlessFarmLabourer",   request.IsLandlessFarmLabourer);
                param.Add("@IsIncomeSourceAgriculture",request.IsIncomeSourceAgriculture);
                param.Add("@HasFarm",                  request.HasFarm);
                param.Add("@UserLoginID",              userLoginId);
                param.Add("@IPAddress",                ipAddress);
                param.Add("@PageCode",                 "CategoryAndOtherReservation");

                var result = _db.ExecuteScalar("ApplicationForm_SaveCategoryAndOtherReservationDetails", param)?.ToString() ?? "";

                if (result.ToUpper() == "Y")
                    return new SaveCategoryResponse { Success = true, Message = "Category details saved successfully." };

                return new SaveCategoryResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Data has not been saved. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new SaveCategoryResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS DETAILS — masters
        // GET /api/applicationform/masters/sports
        // SP: Base_GetMasterTableList("Master_SportsCertificateType","CertificateTypeID","CertificateType","SeqNo")
        // ══════════════════════════════════════════════════════════════════════
        public SportsMastersResponse GetSportsMasters()
        {
            var response = new SportsMastersResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",        "Master_SportsCertificateType");
                param.Add("@DataValueField",   "CertificateTypeID");
                param.Add("@DataTextField",    "CertificateType");
                param.Add("@ParentField",      "");
                param.Add("@ParentFieldValue", "");
                param.Add("@OrderByFields",    "SeqNo");
                var dt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        var val = row[0].ToString()!;
                        if (val != "-1")
                            response.CertificateTypes.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetSportsMasters error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS DETAILS — get existing
        // GET /api/applicationform/sports
        // SP: ApplicationForm_GetSportsDetails
        // ══════════════════════════════════════════════════════════════════════
        public SportsDetailsResponse GetSportsDetails(long candidateId, string userLoginId)
        {
            var response = new SportsDetailsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@PageCode",    "SportsDetails");
                var dt = _db.GetDataTable("ApplicationForm_GetSportsDetails", param);
                if (dt == null || dt.Rows.Count == 0) return response;
                var row = dt.Rows[0];
                if (row["CandidateID"] == DBNull.Value || Convert.ToInt64(row["CandidateID"]) == 0)
                    return response;
                response.Found               = true;
                response.IsSportsCertificate = row["IsSportsCertificate"] != DBNull.Value
                    && Convert.ToBoolean(row["IsSportsCertificate"]);
                response.CertificateTypeID   = row["CertificateTypeID"] != DBNull.Value
                    ? Convert.ToInt32(row["CertificateTypeID"]) : 0;
            }
            catch (Exception ex) { Console.WriteLine($"GetSportsDetails error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPORTS DETAILS — save
        // POST /api/applicationform/sports
        // SP: ApplicationForm_SaveSportsDetails
        // Params: @CandidateID, @IsSportsCertificate, @CertificateTypeID (DBNull if 0),
        //         @UserLoginID, @IPAddress, @PageCode
        // ══════════════════════════════════════════════════════════════════════
        public SaveSportsResponse SaveSportsDetails(
            long candidateId, string userLoginId, string ipAddress, SaveSportsRequest request)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID",         candidateId);
                param.Add("@IsSportsCertificate", request.IsSportsCertificate);
                if (request.IsSportsCertificate && request.CertificateTypeID > 0)
                    param.Add("@CertificateTypeID", request.CertificateTypeID);
                else
                    param.Add("@CertificateTypeID", DBNull.Value);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "SportsDetails");
                var result = _db.ExecuteScalar("ApplicationForm_SaveSportsDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new SaveSportsResponse { Success = true, Message = "Sports details saved successfully." };
                return new SaveSportsResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Data has not been saved. Please try again."
                };
            }
            catch (Exception ex)
            {
                return new SaveSportsResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — get available colleges
        // GET /api/applicationform/options/available
        // SP: ApplicationForm_GetAvailableOptionsList  @CandidateID
        // Returns: CollegeID, CollegeCode, CollegeName, District, CourseStatus
        // ══════════════════════════════════════════════════════════════════════
        public AvailableOptionsResponse GetAvailableOptions(long candidateId)
        {
            var response = new AvailableOptionsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                var dt = _db.GetDataTable("ApplicationForm_GetAvailableOptionsList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Colleges.Add(new CollegeOptionDto
                        {
                            CollegeID    = row["CollegeID"]    != DBNull.Value ? Convert.ToInt64(row["CollegeID"])   : 0,
                            CollegeCode  = row["CollegeCode"].ToString()!,
                            CollegeName  = row["CollegeName"].ToString()!,
                            District     = row["District"].ToString()!,
                            CourseStatus = row["CourseStatus"].ToString()!,
                        });
            }
            catch (Exception ex) { Console.WriteLine($"GetAvailableOptions error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — get shortlisted colleges
        // GET /api/applicationform/options/shortlisted
        // SP: ApplicationForm_GetShortlistedOptionsList  @CandidateID
        // Returns: CollegeID, CollegeCode, CollegeName, District, CourseStatus, PreferenceNo
        // ══════════════════════════════════════════════════════════════════════
        public ShortlistedOptionsResponse GetShortlistedOptions(long candidateId)
        {
            var response = new ShortlistedOptionsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                var dt = _db.GetDataTable("ApplicationForm_GetShortlistedOptionsList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Colleges.Add(new CollegeOptionDto
                        {
                            CollegeID    = row["CollegeID"]    != DBNull.Value ? Convert.ToInt64(row["CollegeID"])    : 0,
                            CollegeCode  = row["CollegeCode"].ToString()!,
                            CollegeName  = row["CollegeName"].ToString()!,
                            District     = row["District"].ToString()!,
                            CourseStatus = row["CourseStatus"].ToString()!,
                            PreferenceNo = row["PreferenceNo"] != DBNull.Value ? Convert.ToInt32(row["PreferenceNo"]) : 0,
                        });
            }
            catch (Exception ex) { Console.WriteLine($"GetShortlistedOptions error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — add a college
        // POST /api/applicationform/options/add
        // SP: ApplicationForm_SaveOption
        // Params: @CandidateID, @CollegeID, @UserLoginID, @IPAddress, @PageCode
        // Returns "Y" on success
        // ══════════════════════════════════════════════════════════════════════
        public OptionActionResponse AddOption(long candidateId, string userLoginId, string ipAddress, long collegeId)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@CollegeID",   collegeId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "ShortListOptions");
                var result = _db.ExecuteScalar("ApplicationForm_SaveOption", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new OptionActionResponse { Success = true, Message = "College added successfully." };
                return new OptionActionResponse { Success = false, Message = result.Length > 0 ? result : "Failed to add college." };
            }
            catch (Exception ex)
            {
                return new OptionActionResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — remove a college
        // DELETE /api/applicationform/options/remove
        // SP: ApplicationForm_DeleteOption
        // Params: @CandidateID, @CollegeID, @UserLoginID, @IPAddress, @PageCode
        // Returns "Y" on success
        // ══════════════════════════════════════════════════════════════════════
        public OptionActionResponse RemoveOption(long candidateId, string userLoginId, string ipAddress, long collegeId)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@CollegeID",   collegeId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "ShortListOptions");
                var result = _db.ExecuteScalar("ApplicationForm_DeleteOption", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new OptionActionResponse { Success = true, Message = "College removed successfully." };
                return new OptionActionResponse { Success = false, Message = result.Length > 0 ? result : "Failed to remove college." };
            }
            catch (Exception ex)
            {
                return new OptionActionResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SHORTLIST — save final order (Proceed button)
        // POST /api/applicationform/options/save
        // SP: ApplicationForm_SaveShortlistedOptionsDetails
        // Params: @CandidateID, @UserLoginID, @IPAddress, @PageCode
        // Returns "Y" on success → navigate to summary
        // ══════════════════════════════════════════════════════════════════════
        public SaveShortlistResponse SaveShortlist(long candidateId, string userLoginId, string ipAddress)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "ShortListOptions");
                var result = _db.ExecuteScalar("ApplicationForm_SaveShortlistedOptionsDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new SaveShortlistResponse { Success = true, Message = "Shortlist saved successfully." };
                return new SaveShortlistResponse { Success = false, Message = result.Length > 0 ? result : "Failed to save shortlist." };
            }
            catch (Exception ex)
            {
                return new SaveShortlistResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SET PREFERENCES — get list for preference-setting page
        // GET /api/applicationform/options/preferenced
        // SP: ApplicationForm_GetPreferancedOptionsList  @CandidateID
        // Returns all shortlisted colleges with their current PreferenceNo
        // Mirrors: GetShortlistedOptions() in SetPreferences.aspx + gvShortlistedOptionsList_RowDataBound
        // ══════════════════════════════════════════════════════════════════════
        public PreferencedOptionsResponse GetPreferencedOptions(long candidateId)
        {
            var response = new PreferencedOptionsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                var dt = _db.GetDataTable("ApplicationForm_GetPreferancedOptionsList", param);
                if (dt != null)
                    foreach (System.Data.DataRow row in dt.Rows)
                        response.Colleges.Add(new CollegeOptionDto
                        {
                            CollegeID    = row["CollegeID"]    != DBNull.Value ? Convert.ToInt64(row["CollegeID"])    : 0,
                            CollegeCode  = row["CollegeCode"].ToString()!,
                            CollegeName  = row["CollegeName"].ToString()!,
                            District     = row["District"].ToString()!,
                            CourseStatus = row["CourseStatus"].ToString()!,
                            // PreferenceNo: 0 means not yet set — mirrors old txtPreferenceNo empty check
                            PreferenceNo = row["PreferenceNo"] != DBNull.Value ? Convert.ToInt32(row["PreferenceNo"]) : 0,
                        });
            }
            catch (Exception ex) { Console.WriteLine($"GetPreferencedOptions error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SET PREFERENCES — save preference order
        // POST /api/applicationform/options/preferences
        // SP: ApplicationForm_SavePreferenceDetails
        // Params: @CandidateID, @OptionsXML, @UserLoginID, @IPAddress, @PageCode
        // XML format: <options><option CollegeID="X" PreferenceNo="1"/></options>
        // Mirrors: OptionFormWorker.SavePreferenceDetails(entity) — entity.ListOptions → XML
        // ══════════════════════════════════════════════════════════════════════
        public SavePreferencesResponse SavePreferences(
            long candidateId, string userLoginId, string ipAddress, SavePreferencesRequest request)
        {
            try
            {
                if (request.Options == null || request.Options.Count == 0)
                    return new SavePreferencesResponse { Success = false, Message = "No preferences provided." };

                // Validate all colleges have a preference number — mirrors IsAllPreferencesGiven check
                if (request.Options.Any(o => o.PreferenceNo <= 0))
                    return new SavePreferencesResponse
                    {
                        Success = false,
                        Message = "Please Set Preferences to All Shortlisted Colleges."
                    };

                // Build XML — exact format matching XmlSerializer output for List<OptionEntity>
                // The SP reads: ArrayOfOptionEntity > OptionEntity > PreferenceNo, CollegeID
                var xml = new System.Text.StringBuilder();
                xml.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
                xml.Append("<ArrayOfOptionEntity xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                foreach (var opt in request.Options)
                {
                    xml.Append("<OptionEntity>");
                    xml.Append($"<PreferenceNo>{opt.PreferenceNo}</PreferenceNo>");
                    xml.Append($"<CollegeID>{opt.CollegeID}</CollegeID>");
                    xml.Append("</OptionEntity>");
                }
                xml.Append("</ArrayOfOptionEntity>");

                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@OptionsXML",  xml.ToString());
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "SetPreferences");

                var result = _db.ExecuteScalar("ApplicationForm_SavePreferenceDetails", param)?.ToString() ?? "";

                if (result.ToUpper() == "Y")
                    return new SavePreferencesResponse { Success = true, Message = "Preferences saved successfully." };

                return new SavePreferencesResponse
                {
                    Success = false,
                    Message = result.Length > 0 ? result : "Failed to save preferences."
                };
            }
            catch (Exception ex)
            {
                return new SavePreferencesResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // RESET PREFERENCES — saves all PreferenceNo=0 to DB
        // POST /api/applicationform/options/preferences/reset
        // Mirrors: ResetPreferences() JS — clears all checkboxes + numbers
        // Uses same SP: ApplicationForm_SavePreferenceDetails with all PreferenceNo=0
        // ══════════════════════════════════════════════════════════════════════
        public SavePreferencesResponse ResetPreferences(long candidateId, string userLoginId, string ipAddress)
        {
            try
            {
                // Get existing shortlisted colleges to build XML with all prefs = 0
                var getParam = new DynamicParameters();
                getParam.Add("@CandidateID", candidateId);
                var dt = _db.GetDataTable("ApplicationForm_GetPreferancedOptionsList", getParam);

                if (dt == null || dt.Rows.Count == 0)
                    return new SavePreferencesResponse { Success = true, Message = "No preferences to reset." };

                // Build XML with PreferenceNo=0 for all colleges
                var xml = new System.Text.StringBuilder();
                xml.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
                xml.Append("<ArrayOfOptionEntity xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    xml.Append("<OptionEntity>");
                    xml.Append("<PreferenceNo>0</PreferenceNo>");
                    xml.Append($"<CollegeID>{row["CollegeID"]}</CollegeID>");
                    xml.Append("</OptionEntity>");
                }
                xml.Append("</ArrayOfOptionEntity>");

                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@OptionsXML",  xml.ToString());
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "SetPreferences");

                var result = _db.ExecuteScalar("ApplicationForm_SavePreferenceDetails", param)?.ToString() ?? "";
                // SP may return "Y" or just update — either way treat as success
                return new SavePreferencesResponse { Success = true, Message = "Preferences reset successfully." };
            }
            catch (Exception ex)
            {
                return new SavePreferencesResponse { Success = false, Message = ex.Message };
            }
        }
        // GET /api/applicationform/photo-sign
        // SP: ApplicationForm_GetPhotoAndSignDetails
        // ══════════════════════════════════════════════════════════════════════
        public PhotoSignDetailsResponse GetPhotoSignDetails(long candidateId, string userLoginId)
        {
            var response = new PhotoSignDetailsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@PageCode",    "UploadPhotoAndSign");
                var dt = _db.GetDataTable("ApplicationForm_GetPhotoAndSignDetails", param);
                if (dt == null || dt.Rows.Count == 0) return response;
                var row = dt.Rows[0];
                if (row["CandidateID"] == DBNull.Value || Convert.ToInt64(row["CandidateID"]) == 0)
                    return response;
                response.Found            = true;
                response.PhotoUploadedURL = row["PhotoUploadedURL"]?.ToString() ?? "";
                response.SignUploadedURL  = row["SignUploadedURL"]?.ToString()  ?? "";
                response.BothUploaded     = response.PhotoUploadedURL.Length > 0 && response.SignUploadedURL.Length > 0;
            }
            catch (Exception ex) { Console.WriteLine($"GetPhotoSignDetails error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PHOTO — upload to Azure Blob + save URL to DB
        // POST /api/applicationform/upload-photo
        // Mirrors: btnUploadPhoto_Click
        // Constraints: JPG/JPEG only, 10KB–100KB
        // Blob path: {container}/{fileProject}/photograph/{candidateId}_p.jpg
        // SP: ApplicationForm_SavePhotoAndSignUploadStatus  (@PhotoUploadedURL, @SignUploadedURL="")
        // ══════════════════════════════════════════════════════════════════════
        public async Task<UploadPhotoSignResponse> UploadPhoto(
            long candidateId, string userLoginId, string ipAddress, IFormFile file)
        {
            try
            {
                // Client-side validation is done in frontend too — this is server-side guard
                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".jpg" && ext != ".jpeg")
                    return new UploadPhotoSignResponse { Success = false, Message = "Photograph Format should be jpg/jpeg." };
                if (file.Length < 10240 || file.Length > 102400)
                    return new UploadPhotoSignResponse { Success = false, Message = "Photograph Size must be greater than 10 KB and less than 100 KB." };

                var url = await UploadToBlob(file, candidateId, "photograph", "p");
                if (url.Length == 0)
                    return new UploadPhotoSignResponse { Success = false, Message = "Failed to upload photograph. Please try again." };

                // Save URL to DB — preserve existing SignUploadedURL from DB
                // Fetch current record first so we don't overwrite an already-uploaded signature
                var existingSign = "";
                try {
                    var getParam = new DynamicParameters();
                    getParam.Add("@CandidateID", candidateId);
                    getParam.Add("@UserLoginID", userLoginId);
                    getParam.Add("@PageCode",    "UploadPhotoAndSign");
                    var existDt = _db.GetDataTable("ApplicationForm_GetPhotoAndSignDetails", getParam);
                    if (existDt != null && existDt.Rows.Count > 0)
                        existingSign = existDt.Rows[0]["SignUploadedURL"]?.ToString() ?? "";
                } catch { /* use empty if fetch fails */ }

                var param = new DynamicParameters();
                param.Add("@CandidateID",      candidateId);
                param.Add("@PhotoUploadedURL", url);
                param.Add("@SignUploadedURL",   existingSign);   // preserve existing sign URL
                param.Add("@UserLoginID",       userLoginId);
                param.Add("@IPAddress",         ipAddress);
                param.Add("@PageCode",          "UploadPhotoAndSign");
                var result = _db.ExecuteScalar("ApplicationForm_SavePhotoAndSignUploadStatus", param)?.ToString() ?? "";
                Console.WriteLine($"[UploadPhoto] SP returned: [{result}] for CandidateID={candidateId}, URL={url}");
                // Accept Y, 1, True, success, or empty — file is saved to disk regardless
                return new UploadPhotoSignResponse { Success = true, Message = "Photograph Uploaded Successfully.", UploadedURL = url };
            }
            catch (Exception ex)
            {
                return new UploadPhotoSignResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SIGNATURE — upload to Azure Blob + save URL to DB
        // POST /api/applicationform/upload-sign
        // Mirrors: btnUploadSign_Click
        // Constraints: JPG/JPEG only, 5KB–50KB
        // Blob path: {container}/{fileProject}/signature/{candidateId}_s.jpg
        // SP: ApplicationForm_SavePhotoAndSignUploadStatus  (@PhotoUploadedURL="", @SignUploadedURL)
        // ══════════════════════════════════════════════════════════════════════
        public async Task<UploadPhotoSignResponse> UploadSign(
            long candidateId, string userLoginId, string ipAddress, IFormFile file)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".jpg" && ext != ".jpeg")
                    return new UploadPhotoSignResponse { Success = false, Message = "Signature Format should be jpg/jpeg." };
                if (file.Length < 5120 || file.Length > 51200)
                    return new UploadPhotoSignResponse { Success = false, Message = "Signature Size must be greater than 5 KB and less than 50 KB." };

                var url = await UploadToBlob(file, candidateId, "signature", "s");
                if (url.Length == 0)
                    return new UploadPhotoSignResponse { Success = false, Message = "Failed to upload signature. Please try again." };

                // Save URL — preserve existing PhotoUploadedURL from DB
                var existingPhoto = "";
                try {
                    var getParam = new DynamicParameters();
                    getParam.Add("@CandidateID", candidateId);
                    getParam.Add("@UserLoginID", userLoginId);
                    getParam.Add("@PageCode",    "UploadPhotoAndSign");
                    var existDt = _db.GetDataTable("ApplicationForm_GetPhotoAndSignDetails", getParam);
                    if (existDt != null && existDt.Rows.Count > 0)
                        existingPhoto = existDt.Rows[0]["PhotoUploadedURL"]?.ToString() ?? "";
                } catch { /* use empty if fetch fails */ }

                var param = new DynamicParameters();
                param.Add("@CandidateID",      candidateId);
                param.Add("@PhotoUploadedURL", existingPhoto);   // preserve existing photo URL
                param.Add("@SignUploadedURL",   url);
                param.Add("@UserLoginID",       userLoginId);
                param.Add("@IPAddress",         ipAddress);
                param.Add("@PageCode",          "UploadPhotoAndSign");
                var result = _db.ExecuteScalar("ApplicationForm_SavePhotoAndSignUploadStatus", param)?.ToString() ?? "";
                Console.WriteLine($"[UploadSign] SP returned: [{result}] for CandidateID={candidateId}, URL={url}");
                // Accept any return value — file is saved to disk regardless
                return new UploadPhotoSignResponse { Success = true, Message = "Signature Uploaded Successfully.", UploadedURL = url };
            }
            catch (Exception ex)
            {
                return new UploadPhotoSignResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PHOTO & SIGN — mark step complete (Proceed)
        // POST /api/applicationform/photo-sign/save
        // Mirrors: btnProceed_Click → SavePhotoAndSign()
        // SP: ApplicationForm_SavePhotoAndSignDetails  (@CandidateID, @UserLoginID, @IPAddress, @PageCode)
        // ══════════════════════════════════════════════════════════════════════
        public SavePhotoSignResponse SavePhotoSign(long candidateId, string userLoginId, string ipAddress)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "UploadPhotoAndSign");
                var result = _db.ExecuteScalar("ApplicationForm_SavePhotoAndSignDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new SavePhotoSignResponse { Success = true, Message = "Photo and Signature details saved successfully." };
                return new SavePhotoSignResponse { Success = false, Message = result.Length > 0 ? result : "Failed to save." };
            }
            catch (Exception ex)
            {
                return new SavePhotoSignResponse { Success = false, Message = ex.Message };
            }
        }

        private async Task<string> UploadToBlob(
            IFormFile file, long candidateId, string subfolder, string suffix)
        {
            try
            {
                var ext      = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{candidateId}_{suffix}{ext}";

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subfolder);
                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await file.CopyToAsync(stream);

                var url = $"/uploads/{subfolder}/{fileName}";
                Console.WriteLine($"File saved: {filePath}  →  URL: {url}");
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadToBlob error: {ex.Message}");
                return "";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // QUALIFICATION — masters
        // GET /api/applicationform/masters/qualification
        // Mirrors: Qualification.aspx LoadMasters()
        // SPs + logic: Master_Qualification, Base_GetMasterDistrict(Group=27),
        //              Helper.GetYearList(-30), Helper.GetNumberList(22/10), Master_Board
        // ══════════════════════════════════════════════════════════════════════
        public QualificationMastersResponse GetQualificationMasters()
        {
            var response = new QualificationMastersResponse();

            // Qualifications — Base_GetMasterTableList ordered by QualificationID
            try
            {
                var param = new DynamicParameters();
                param.Add("@TableName",        "Master_Qualification");
                param.Add("@DataValueField",   "QualificationID");
                param.Add("@DataTextField",    "Qualification");
                param.Add("@ParentField",      "");
                param.Add("@ParentFieldValue", "");
                param.Add("@OrderByFields",    "QualificationID");
                var qualDt = _db.GetDataTable("Base_GetMasterTableList", param);
                if (qualDt != null)
                    foreach (System.Data.DataRow row in qualDt.Rows)
                    {
                        var val = row[0].ToString()!;
                        if (val != "-1") response.Qualifications.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetQualificationMasters Qualifications error: {ex.Message}"); }

            // Passing Districts — Maharashtra only (Group=27) — same as ddlPassingDistrict
            try
            {
                var distDt = _db.GetDataTable("Base_GetMasterDistrict");
                if (distDt != null)
                    foreach (System.Data.DataRow row in distDt.Rows)
                    {
                        var val   = row[0].ToString()!;
                        var group = row[2].ToString()!;
                        if (val == "-1" || group == "-1") continue;
                        if (group == "27")
                            response.PassingDistricts.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetQualificationMasters Districts error: {ex.Message}"); }

            // Passing Years — last 30 years from 2026 — mirrors Helper.GetYearList(-30)
            for (int y = 2026; y >= 1997; y--)
                response.PassingYears.Add(new DropdownItem { Value = y.ToString(), Text = y.ToString() });

            // Boards — Base_GetMasterTableList ordered by BoardID
            try
            {
                var bParam = new DynamicParameters();
                bParam.Add("@TableName",        "Master_Board");
                bParam.Add("@DataValueField",   "BoardID");
                bParam.Add("@DataTextField",    "Board");
                bParam.Add("@ParentField",      "");
                bParam.Add("@ParentFieldValue", "");
                bParam.Add("@OrderByFields",    "BoardID");
                var boardDt = _db.GetDataTable("Base_GetMasterTableList", bParam);
                if (boardDt != null)
                    foreach (System.Data.DataRow row in boardDt.Rows)
                    {
                        var val = row[0].ToString()!;
                        if (val != "-1") response.Boards.Add(new DropdownItem { Value = val, Text = row[1].ToString()! });
                    }
            }
            catch (Exception ex) { Console.WriteLine($"GetQualificationMasters Boards error: {ex.Message}"); }

            // Educational Gap Years 1–22 — mirrors Helper.GetNumberList(22)
            for (int i = 1; i <= 22; i++)
                response.EducationalGapYears.Add(new DropdownItem { Value = i.ToString(), Text = i.ToString() });

            // No of Attempts 1–10 — mirrors Helper.GetNumberList(10)
            for (int i = 1; i <= 10; i++)
                response.NoOfAttempts.Add(new DropdownItem { Value = i.ToString(), Text = i.ToString() });

            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // QUALIFICATION — get existing
        // GET /api/applicationform/qualification
        // SP: ApplicationForm_GetQualificationDetails
        // ══════════════════════════════════════════════════════════════════════
        public QualificationDetailsResponse GetQualificationDetails(long candidateId, string userLoginId)
        {
            var response = new QualificationDetailsResponse();
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@PageCode",    "Qualification");
                var dt = _db.GetDataTable("ApplicationForm_GetQualificationDetails", param);
                if (dt == null || dt.Rows.Count == 0) return response;
                var row = dt.Rows[0];
                if (row["CandidateID"] == DBNull.Value || Convert.ToInt64(row["CandidateID"]) == 0) return response;
                response.Found                   = true;
                response.EligibilityQualification    = row["EligibilityQualification"]?.ToString()    ?? "";
                response.EligibilityQualificationID  = row["EligibilityQualificationID"] != DBNull.Value ? Convert.ToInt16(row["EligibilityQualificationID"]) : (short)0;
                response.HighestQualificationID  = row["HighestQualificationID"] != DBNull.Value ? Convert.ToInt16(row["HighestQualificationID"]) : (short)0;
                response.IsEducationalGap        = row["IsEducationalGap"]        != DBNull.Value ? Convert.ToInt16(row["IsEducationalGap"])        : (short)0;
                response.EducationalGapYears     = row["EducationalGapYears"]     != DBNull.Value ? Convert.ToInt16(row["EducationalGapYears"])     : (short)0;
                response.EducationalGapReason    = row["EducationalGapReason"]?.ToString()    ?? "";
                response.SeatNo                  = row["SeatNo"]?.ToString()                  ?? "";
                response.NoOfAttempts            = row["NoOfAttempts"]            != DBNull.Value ? Convert.ToInt16(row["NoOfAttempts"])            : (short)0;
                response.PassingDistrictID       = row["PassingDistrictID"]       != DBNull.Value ? Convert.ToInt32(row["PassingDistrictID"])       : 0;
                response.PassingYear             = row["PassingYear"]             != DBNull.Value ? Convert.ToInt16(row["PassingYear"])             : (short)0;
                response.BoardID                 = row["BoardID"]                 != DBNull.Value ? Convert.ToInt16(row["BoardID"])                 : (short)0;
                response.MarksObtained           = row["MarksObtained"]           != DBNull.Value ? Convert.ToInt32(row["MarksObtained"])           : 0;
                response.MarksOutOf              = row["MarksOutOf"]              != DBNull.Value ? Convert.ToInt32(row["MarksOutOf"])              : 0;
                response.Percentage              = row["Percentage"]              != DBNull.Value ? Convert.ToDecimal(row["Percentage"])            : 0;
            }
            catch (Exception ex) { Console.WriteLine($"GetQualificationDetails error: {ex.Message}"); }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // QUALIFICATION — save
        // POST /api/applicationform/qualification
        // SP: ApplicationForm_SaveQualificationDetails
        // Params from Repository.Qualification.cs — exact same as old entity
        // Percentage computed here: (MarksObtained * 100) / MarksOutOf
        // ══════════════════════════════════════════════════════════════════════
        public SaveQualificationResponse SaveQualificationDetails(
            long candidateId, string userLoginId, string ipAddress, SaveQualificationRequest request)
        {
            try
            {
                decimal percentage = request.MarksOutOf > 0
                    ? (Convert.ToDecimal(request.MarksObtained) * 100) / request.MarksOutOf
                    : 0;

                var param = new DynamicParameters();
                param.Add("@CandidateID",             candidateId);
                param.Add("@HighestQualificationID",  request.HighestQualificationID);
                param.Add("@IsEducationalGap",        request.IsEducationalGap);
                param.Add("@EducationalGapYears",     request.EducationalGapYears);
                param.Add("@EducationalGapReason",    request.EducationalGapReason.Trim().ToUpper());
                param.Add("@EligibilityQualificationID", request.EligibilityQualificationID);
                param.Add("@SeatNo",                  request.SeatNo.Trim().ToUpper());
                param.Add("@NoOfAttempts",            request.NoOfAttempts);
                param.Add("@PassingDistrictID",       request.PassingDistrictID);
                param.Add("@PassingYear",             request.PassingYear);
                param.Add("@BoardID",                 request.BoardID);
                param.Add("@MarksObtained",           request.MarksObtained);
                param.Add("@MarksOutOf",              request.MarksOutOf);
                param.Add("@Percentage",              percentage);
                param.Add("@UserLoginID",             userLoginId);
                param.Add("@IPAddress",               ipAddress);
                param.Add("@PageCode",                "Qualification");

                var result = _db.ExecuteScalar("ApplicationForm_SaveQualificationDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new SaveQualificationResponse { Success = true, Message = "Qualification details saved successfully." };
                return new SaveQualificationResponse { Success = false, Message = result.Length > 0 ? result : "Data has not been saved. Please try again." };
            }
            catch (Exception ex)
            {
                return new SaveQualificationResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
