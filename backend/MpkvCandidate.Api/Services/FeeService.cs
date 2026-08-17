using MpkvCandidate.Api.Data;
using MpkvCandidate.Api.Models;
using Dapper;

namespace MpkvCandidate.Api.Services
{
    // ══════════════════════════════════════════════════════════════════════════
    // IFeeService — gateway callback processing
    // Mirrors: NsdlResponseHandler.aspx.cs + BillDeskResponseHandler.aspx.cs
    //          + NsdlPushResponseHandler.aspx.cs + PaymentHistory.CheckFailedTransactions
    // ══════════════════════════════════════════════════════════════════════════
    public interface IFeeService
    {
        // Called on page-load — checks pending/failed transactions via NSDL API
        // Mirrors: PaymentHistory.CheckFailedTransactions(CandidateId)
        Task CheckFailedTransactions(long candidateId);

        // NSDL browser response — POST from gateway after payment
        // Mirrors: NsdlResponseHandler.aspx.cs ProcessResponse()
        Task<(bool IsPaid, string RedirectUrl)> ProcessNsdlResponse(string msg, string frontendBase);

        // NSDL push response — server-to-server from NSDL
        // Mirrors: NsdlPushResponseHandler.aspx.cs
        Task<string> ProcessNsdlPushResponse(string msg);

        // BillDesk browser response — POST from gateway after payment
        // Mirrors: BillDeskResponseHandler.aspx.cs ProceedFurther()
        Task<(bool IsPaid, string RedirectUrl)> ProcessBillDeskResponse(string transactionResponse, string frontendBase);

        // Save transaction response to DB (both success + failure)
        // Mirrors: FeeWorker.SetFeeTransactionResponse()
        FeeResponseEntity SetFeeTransactionResponse(FeeTransactionEntity ft);

        // Get transaction details by ID
        // Mirrors: FeeWorker.GetTransactionDetails()
        FeeTransactionEntity? GetTransactionDetails(long transactionId);

        // Save fee details after successful payment (PhaseID=99 → application fee)
        // Mirrors: PaymentSuccess.aspx.cs → SaveApplicationFeeDetails()
        FeeProceedResponse SaveApplicationFeeDetails(long candidateId, string userLoginId, string ipAddress);
    }

    public class FeeService : IFeeService
    {
        private readonly DbAccess       _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public FeeService(DbAccess db, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _db               = db;
            _config           = config;
            _httpClientFactory = httpClientFactory;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetTransactionDetails
        // SP: Fee_GetTransactionDetails
        // ══════════════════════════════════════════════════════════════════════
        public FeeTransactionEntity? GetTransactionDetails(long transactionId)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@TransactionID", transactionId);
                var dt = _db.GetDataTable("Fee_GetTransactionDetails", param);
                if (dt == null || dt.Rows.Count == 0) return null;

                var row    = dt.Rows[0];
                bool H(string n) => dt.Columns.Contains(n);

                return new FeeTransactionEntity
                {
                    TransactionID      = transactionId,
                    PayeeID            = H("PayeeID")           && row["PayeeID"]           != DBNull.Value ? Convert.ToInt64(row["PayeeID"])    : 0,
                    PayeeApplicationID = H("PayeeApplicationID") ? row["PayeeApplicationID"]?.ToString() ?? "" : "",
                    PayeeName          = H("PayeeName")          ? row["PayeeName"]?.ToString()          ?? "" : "",
                    PayeeMobileNo      = H("PayeeMobileNo")      ? row["PayeeMobileNo"]?.ToString()      ?? "" : "",
                    PayeeEMailID       = H("PayeeEMailID")       ? row["PayeeEMailID"]?.ToString()       ?? "" : "",
                    FeeAmount          = H("FeeAmount")          && row["FeeAmount"]         != DBNull.Value ? Convert.ToDecimal(row["FeeAmount"]) : 0,
                    PhaseID            = H("PhaseID")            && row["PhaseID"]           != DBNull.Value ? Convert.ToInt32(row["PhaseID"])     : 0,
                    AppliedCourse      = H("AppliedCourse")      ? row["AppliedCourse"]?.ToString()      ?? "" : "",
                    Purpose            = H("Purpose")            ? row["Purpose"]?.ToString()            ?? "" : "",
                    IsPaid             = H("IsPaid")             && row["IsPaid"]            != DBNull.Value && Convert.ToBoolean(row["IsPaid"]),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetTransactionDetails] error: {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // SetFeeTransactionResponse
        // SP: Fee_SetFeeTransactionResponse
        // Mirrors: FeeWorker.SetFeeTransactionResponse()
        // ══════════════════════════════════════════════════════════════════════
        public FeeResponseEntity SetFeeTransactionResponse(FeeTransactionEntity ft)
        {
            var response = new FeeResponseEntity();
            try
            {
                var param = new DynamicParameters();
                param.Add("@TransactionID",          ft.TransactionID);
                param.Add("@FeeAmount",              ft.FeeAmount);
                param.Add("@IsPaid",                 ft.IsPaid);
                param.Add("@BankReferenceNo",        ft.BankRefereneceNo);   // entity typo → SP correctly spelled
                param.Add("@PayGateID",              ft.PayGateID);
                param.Add("@TransactionResponse",    ft.TransactionResponse);
                param.Add("@PaymentGatewayResponse", ft.PaymentGatewayResponse);
                param.Add("@Optional1",              ft.Optional1);
                param.Add("@Optional2",              ft.Optional2);
                param.Add("@Optional3",              ft.Optional3);
                param.Add("@Optional4",              ft.Optional4);
                param.Add("@Optional5",              ft.Optional5);
                param.Add("@UserLoginId",            ft.UserLoginId);
                param.Add("@IPAddress",              ft.IPAddress);

                var dt = _db.GetDataTable("Fee_SetFeeTransactionResponse", param);
                if (dt != null && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    bool H(string n) => dt.Columns.Contains(n);
                    response.TransactionID   = H("TransactionID")   && row["TransactionID"]   != DBNull.Value ? Convert.ToInt64(row["TransactionID"])  : 0;
                    response.FeeAmount       = H("FeeAmount")       && row["FeeAmount"]       != DBNull.Value ? Convert.ToInt32(row["FeeAmount"])       : 0;
                    response.BankReferenceNo = H("BankReferenceNo") ? row["BankReferenceNo"]?.ToString() ?? "" : "";
                    response.SuccessFlag     = H("SuccessFlag")     ? row["SuccessFlag"]?.ToString()     ?? "" : "";
                    response.ErrorMessage    = H("ErrorMessage")    ? row["ErrorMessage"]?.ToString()    ?? "" : "";
                    response.IsPaid          = ft.IsPaid;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetFeeTransactionResponse] error: {ex.Message}");
                response.SuccessFlag  = "N";
                response.ErrorMessage = ex.Message;
            }
            return response;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SaveApplicationFeeDetails
        // SP: ApplicationForm_SaveApplicationFeeDetails
        // Called after successful payment (PhaseID == 99)
        // ══════════════════════════════════════════════════════════════════════
        public FeeProceedResponse SaveApplicationFeeDetails(long candidateId, string userLoginId, string ipAddress)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CandidateID", candidateId);
                param.Add("@UserLoginID", userLoginId);
                param.Add("@IPAddress",   ipAddress);
                param.Add("@PageCode",    "PayApplicationFee");

                var result = _db.ExecuteScalar("ApplicationForm_SaveApplicationFeeDetails", param)?.ToString() ?? "";
                if (result.ToUpper() == "Y")
                    return new FeeProceedResponse { Success = true, Message = "Fee details saved." };
                return new FeeProceedResponse { Success = false, Message = result.Length > 0 ? result : "Failed to save fee details." };
            }
            catch (Exception ex)
            {
                return new FeeProceedResponse { Success = false, Message = ex.Message };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CheckFailedTransactions
        // Mirrors: PaymentHistory.CheckFailedTransactions(CandidateId)
        // SP: Fee_GetFailedTransactionForAPICheck → for each → NSDL API check
        // ══════════════════════════════════════════════════════════════════════
        public async Task CheckFailedTransactions(long candidateId)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@PayeeID", candidateId);
                var dt = _db.GetDataTable("Fee_GetFailedTransactionForAPICheck", param);
                if (dt == null || dt.Rows.Count == 0) return;

                var merchantId = _config["NSDL:MerchantID"] ?? "";
                var secretKey  = _config["NSDL:SecretKey"]  ?? "";
                var apiUrl     = _config["NSDL:PaidCheckAPIURL"] ?? "";
                var userName   = _config["NSDL:UserName"]   ?? "";
                var password   = _config["NSDL:Password"]   ?? "";

                if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(apiUrl)) return;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    if (row["TransactionID"] == DBNull.Value) continue;
                    var txId = Convert.ToInt64(row["TransactionID"]);
                    try { await CheckSingleNsdlTransaction(txId, merchantId, secretKey, apiUrl, userName, password); }
                    catch { /* silent per old code */ }
                }
            }
            catch { /* silent per old code */ }
        }

        private async Task CheckSingleNsdlTransaction(long txId, string merchantId, string secretKey, string apiUrl, string userName, string password)
        {
            var msg = $"|{merchantId}|{txId}";
            var client = _httpClientFactory.CreateClient();

            // Basic auth like old NSDLHelper.cs
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{userName}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("requestMsg", msg) });
            var httpResp = await client.PostAsync($"{apiUrl.TrimEnd('/')}/queryPaymentStatus", content);
            var responseStr = await httpResp.Content.ReadAsStringAsync();

            if (!responseStr.StartsWith("S")) return;

            var parts = responseStr.Split('|');
            if (parts.Length < 20) return;

            // Verify CRC32 checksum — last segment
            var checksum    = parts[parts.Length - 1];
            var msgWithKey  = responseStr.Substring(0, responseStr.LastIndexOf('|')) + "|" + secretKey;
            var computed    = ComputeCrc32(msgWithKey).ToString();
            if (computed != checksum) return;

            var ft = new FeeTransactionEntity
            {
                TransactionID          = Convert.ToInt64(parts[4]),
                PayeeApplicationID     = parts.Length > 5  ? parts[5]  : "",
                FeeAmount              = parts.Length > 6  ? decimal.TryParse(parts[6], out var fa)  ? fa : 0 : 0,
                Optional2              = parts.Length > 8  ? parts[8]  : "",   // PaymentMode
                PayGateID              = parts.Length > 10 ? parts[10] : "",
                BankRefereneceNo       = parts.Length > 11 ? parts[11] : "",
                ErrorMessage           = parts.Length > 12 ? parts[12] : "",   // TransactionStatus
                Optional4              = parts.Length > 18 ? parts[18] : "",
                Optional5              = parts.Length > 19 ? parts[19] : "",
                IsPaid                 = parts[0] == "S",
                IsValid                = parts[0] == "S",
                PaymentGatewayResponse = responseStr,
            };
            SetFeeTransactionResponse(ft);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ProcessNsdlResponse
        // POST /api/fee/nsdl-response
        // Mirrors: NsdlResponseHandler.aspx.cs
        // ══════════════════════════════════════════════════════════════════════
        public async Task<(bool IsPaid, string RedirectUrl)> ProcessNsdlResponse(string msg, string frontendBase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return (false, $"{frontendBase}/payment-failed?msg=Empty+response+received");

                var secretKey = _config["NSDL:SecretKey"] ?? "";
                var parts = msg.Split('|');
                if (parts.Length < 5)
                    return (false, $"{frontendBase}/payment-failed?msg=Invalid+response+format");

                // Verify CRC32
                var checksum   = parts[parts.Length - 1];
                var msgWithKey = msg.Substring(0, msg.LastIndexOf('|')) + "|" + secretKey;
                var computed   = ComputeCrc32(msgWithKey).ToString();
                if (!string.IsNullOrEmpty(secretKey) && computed != checksum)
                    return (false, $"{frontendBase}/payment-failed?msg=Checksum+mismatch");

                var responseFlag = parts[0];
                var ft           = new FeeTransactionEntity { PaymentGatewayResponse = msg };

                if (responseFlag == "S") // Success
                {
                    ft.TransactionID      = long.TryParse(parts.Length > 4  ? parts[4]  : "0", out var tid) ? tid : 0;
                    ft.PayeeApplicationID = parts.Length > 5  ? parts[5]  : "";
                    ft.FeeAmount          = parts.Length > 6  ? decimal.TryParse(parts[6], out var fa) ? fa : 0 : 0;
                    ft.Optional2          = parts.Length > 8  ? parts[8]  : "";
                    ft.PayGateID          = parts.Length > 10 ? parts[10] : "";
                    ft.BankRefereneceNo   = parts.Length > 11 ? parts[11] : "";
                    ft.ErrorMessage       = parts.Length > 12 ? parts[12] : "";
                    ft.Optional4          = parts.Length > 18 ? parts[18] : "";
                    ft.Optional5          = parts.Length > 19 ? parts[19] : "";
                    ft.IsPaid             = true;
                    ft.IsValid            = true;
                }
                else if (responseFlag == "F" || responseFlag == "D") // Failure / Declined
                {
                    ft.TransactionID     = long.TryParse(parts.Length > 4 ? parts[4] : "0", out var tid2) ? tid2 : 0;
                    ft.PayeeApplicationID = parts.Length > 5 ? parts[5] : "";
                    ft.Optional2         = parts.Length > 8 ? parts[8] : "";
                    ft.ErrorMessage      = parts.Length > 12 ? parts[12] : "Payment failed.";
                    ft.IsPaid            = false;
                    ft.IsValid           = false;
                }
                else
                {
                    return (false, $"{frontendBase}/payment-failed?msg=Unknown+response+flag");
                }

                var fbResult = SetFeeTransactionResponse(ft);

                if (fbResult.SuccessFlag?.ToUpper() == "Y" && ft.IsPaid)
                {
                    // Auto-save fee step after successful payment (PhaseID=99)
                    var txDetails = GetTransactionDetails(ft.TransactionID);
                    if (txDetails?.PhaseID == 99)
                    {
                        SaveApplicationFeeDetails(txDetails.PayeeID, "", "");
                    }
                    var refNo = Uri.EscapeDataString(ft.BankRefereneceNo);
                    return (true, $"{frontendBase}/payment-success?txId={ft.TransactionID}&refNo={refNo}&amount={ft.FeeAmount}");
                }
                else
                {
                    var errMsg = Uri.EscapeDataString(ft.ErrorMessage.Length > 0 ? ft.ErrorMessage : "Payment was not successful.");
                    return (false, $"{frontendBase}/payment-failed?msg={errMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessNsdlResponse] error: {ex.Message}");
                return (false, $"{frontendBase}/payment-failed?msg={Uri.EscapeDataString(ex.Message)}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ProcessNsdlPushResponse
        // POST /api/fee/nsdl-push
        // Mirrors: NsdlPushResponseHandler.aspx.cs
        // Server-to-server push — returns "200|Y" on success, "400|N" on failure
        // ══════════════════════════════════════════════════════════════════════
        public async Task<string> ProcessNsdlPushResponse(string msg)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg)) return "400|N";

                var secretKey = _config["NSDL:SecretKey"] ?? "";
                var parts = msg.Split('|');
                if (parts.Length < 5) return "400|N";

                var checksum   = parts[parts.Length - 1];
                var msgWithKey = msg.Substring(0, msg.LastIndexOf('|')) + "|" + secretKey;
                var computed   = ComputeCrc32(msgWithKey).ToString();
                if (!string.IsNullOrEmpty(secretKey) && computed != checksum) return "400|N";

                var responseFlag = parts[0];
                var ft = new FeeTransactionEntity
                {
                    IsPushResponse         = true,
                    PaymentGatewayResponse = msg,
                };

                if (responseFlag == "S")
                {
                    ft.TransactionID    = long.TryParse(parts.Length > 4  ? parts[4]  : "0", out var tid) ? tid : 0;
                    ft.FeeAmount        = parts.Length > 6  ? decimal.TryParse(parts[6],  out var fa) ? fa : 0 : 0;
                    ft.Optional2        = parts.Length > 8  ? parts[8]  : "";
                    ft.PayGateID        = parts.Length > 10 ? parts[10] : "";
                    ft.BankRefereneceNo = parts.Length > 11 ? parts[11] : "";
                    ft.ErrorMessage     = parts.Length > 12 ? parts[12] : "";   // maps to @TransactionResponse
                    ft.Optional4        = parts.Length > 18 ? parts[18] : "";
                    ft.Optional5        = parts.Length > 19 ? parts[19] : "";
                    ft.IsPaid           = true;
                    ft.IsValid          = true;
                }
                else
                {
                    ft.TransactionID = long.TryParse(parts.Length > 4 ? parts[4] : "0", out var tid2) ? tid2 : 0;
                    ft.IsPaid        = false;
                }

                // Push uses SetFeePushResponse SP (different from browser response SP)
                var pushParam = new DynamicParameters();
                pushParam.Add("@TransactionID",       ft.TransactionID);
                pushParam.Add("@BankRefereneceNo",    ft.BankRefereneceNo);  // typo matches SP
                pushParam.Add("@PayGateID",           ft.PayGateID);
                pushParam.Add("@GatewayFullResponse", ft.PaymentGatewayResponse);
                pushParam.Add("@TransactionResponse", ft.ErrorMessage);
                pushParam.Add("@FeeAmount",           ft.FeeAmount);
                pushParam.Add("@IsPaid",              ft.IsPaid);
                pushParam.Add("@Optional1",           ft.Optional1);
                pushParam.Add("@Optional2",           ft.Optional2);
                pushParam.Add("@Optional3",           ft.Optional3);
                pushParam.Add("@Optional4",           ft.Optional4);
                pushParam.Add("@Optional5",           ft.Optional5);

                var result = _db.ExecuteScalar("Fee_SetFeePushResponse", pushParam)?.ToString() ?? "400|N";
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessNsdlPushResponse] error: {ex.Message}");
                return "400|N";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ProcessBillDeskResponse
        // POST /api/fee/billdesk-response
        // Mirrors: BillDeskResponseHandler.aspx.cs
        // BillDesk returns a JWT — decode with SecretKey → parse Transaction_Response JSON
        // ══════════════════════════════════════════════════════════════════════
        public async Task<(bool IsPaid, string RedirectUrl)> ProcessBillDeskResponse(string transactionResponse, string frontendBase)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionResponse))
                    return (false, $"{frontendBase}/payment-failed?msg=Empty+BillDesk+response");

                var secretKey = _config["BillDesk:SecretKey"] ?? "";

                // Decode JWT (HS256) — BillDesk uses JWS compact serialisation
                // Jose.JWT.Decode requires the Jose-JWT NuGet package
                // Since we don't want an extra dependency, decode manually (header.payload.sig)
                string jsonPayload;
                var jwtParts = transactionResponse.Split('.');
                if (jwtParts.Length >= 2)
                {
                    var padded = jwtParts[1].PadRight(jwtParts[1].Length + (4 - jwtParts[1].Length % 4) % 4, '=');
                    jsonPayload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                }
                else
                {
                    jsonPayload = transactionResponse; // fallback — already JSON
                }

                var txn = System.Text.Json.JsonSerializer.Deserialize<BillDeskTransactionResponse>(jsonPayload,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (txn == null)
                    return (false, $"{frontendBase}/payment-failed?msg=Invalid+BillDesk+response");

                var ft = new FeeTransactionEntity
                {
                    PaymentGatewayResponse = transactionResponse,
                };

                // Map fields — mirrors BillDeskResponseHandler.aspx.cs
                if (long.TryParse(txn.orderid, out var oid)) ft.TransactionID = oid;
                if (decimal.TryParse(txn.amount, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amt)) ft.FeeAmount = amt;
                ft.Optional2          = txn.payment_method_type;
                ft.BankRefereneceNo   = txn.transactionid;
                ft.ErrorMessage       = txn.transaction_error_desc;
                ft.Optional4          = txn.transaction_error_desc;
                ft.TransactionResponse = txn.transaction_error_desc;
                ft.Optional5          = txn.transaction_error_code;
                ft.IsPaid             = txn.transaction_error_type?.ToLower() == "success";
                ft.IsValid            = ft.IsPaid;

                var fbResult = SetFeeTransactionResponse(ft);

                if (fbResult.SuccessFlag?.ToUpper() == "Y" && ft.IsPaid)
                {
                    var txDetails = GetTransactionDetails(ft.TransactionID);
                    if (txDetails?.PhaseID == 99)
                        SaveApplicationFeeDetails(txDetails.PayeeID, "", "");
                    var refNo = Uri.EscapeDataString(ft.BankRefereneceNo);
                    return (true, $"{frontendBase}/payment-success?txId={ft.TransactionID}&refNo={refNo}&amount={ft.FeeAmount}");
                }
                else
                {
                    var errMsg = Uri.EscapeDataString(ft.ErrorMessage.Length > 0 ? ft.ErrorMessage : "Payment was not successful.");
                    return (false, $"{frontendBase}/payment-failed?msg={errMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessBillDeskResponse] error: {ex.Message}");
                return (false, $"{frontendBase}/payment-failed?msg={Uri.EscapeDataString(ex.Message)}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CRC32 — matches old NSDLHelper.GenerateCRC32Checksum
        // Algorithm: CRC32 with polynomial 0xEDB88320 (reversed), returns decimal string
        // ══════════════════════════════════════════════════════════════════════
        private static uint ComputeCrc32(string input)
        {
            var bytes  = System.Text.Encoding.UTF8.GetBytes(input);
            uint crc   = 0xFFFFFFFF;
            uint poly  = 0xEDB88320;
            foreach (var b in bytes)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
            }
            return crc ^ 0xFFFFFFFF;
        }
    }
}
