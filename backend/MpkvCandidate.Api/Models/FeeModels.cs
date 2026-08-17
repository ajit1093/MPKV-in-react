namespace MpkvCandidate.Api.Models
{
    // ══════════════════════════════════════════════════════════════════════════
    // FEE TRANSACTION — mirrors FeeTransactionEntity from MPKV Diploma
    // ══════════════════════════════════════════════════════════════════════════

    public class FeeTransactionEntity
    {
        public long    TransactionID             { get; set; }
        public string  AppliedCourse             { get; set; } = string.Empty;
        public int     PhaseID                   { get; set; }
        public string  Purpose                   { get; set; } = string.Empty;
        public long    PayeeID                   { get; set; }
        public string  PayeeApplicationID        { get; set; } = string.Empty;
        public string  PayeeName                 { get; set; } = string.Empty;
        public string  PayeeMobileNo             { get; set; } = string.Empty;
        public string  PayeeEMailID              { get; set; } = string.Empty;
        public decimal FeeAmount                 { get; set; }
        public decimal ServiceCharge             { get; set; }
        public decimal TotalAmount               { get; set; }
        public string  PaymentGateway            { get; set; } = string.Empty;
        public bool    IsValid                   { get; set; }
        public bool    IsPaid                    { get; set; }
        public string  TransactionDate           { get; set; } = string.Empty;
        public string  LastPaymentDate           { get; set; } = string.Empty;
        /// <summary>Intentional typo from old codebase — matches SP param @BankRefereneceNo</summary>
        public string  BankRefereneceNo          { get; set; } = string.Empty;
        public string  PayGateID                 { get; set; } = string.Empty;
        public string  PaymentDate               { get; set; } = string.Empty;
        public string  TransactionResponse       { get; set; } = string.Empty;
        public string  Optional1                 { get; set; } = string.Empty;
        public string  Optional2                 { get; set; } = string.Empty;
        public string  Optional3                 { get; set; } = string.Empty;
        public string  Optional4                 { get; set; } = string.Empty;
        public string  Optional5                 { get; set; } = string.Empty;
        public bool    IsRefundInitiated         { get; set; }
        public bool    IsRefunded                { get; set; }
        public bool    IsChargeBackAccepted      { get; set; }
        public bool    IsPushResponse            { get; set; }
        public bool    IsMainTransaction         { get; set; }
        public bool    IsReconciled              { get; set; }
        public string  PaymentDoneBy             { get; set; } = string.Empty;
        public string  ReceiptURL                { get; set; } = string.Empty;
        public string  TransactionStatus         { get; set; } = string.Empty;
        public string  ErrorMessage              { get; set; } = string.Empty;
        public string  PaymentGatewayResponse    { get; set; } = string.Empty;
        public string  IPAddress                 { get; set; } = string.Empty;
        public string  UserLoginId               { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FEE RESPONSE — mirrors FeeResponseEntity from MPKV Diploma
    // Returned by Fee_SetFeeTransaction and Fee_SetFeeTransactionResponse SPs
    // ══════════════════════════════════════════════════════════════════════════

    public class FeeResponseEntity
    {
        public long   TransactionID     { get; set; }
        public int    FeeAmount         { get; set; }
        public string BankReferenceNo   { get; set; } = string.Empty;
        public string PaymentGatewayURL { get; set; } = string.Empty;
        public string SuccessFlag       { get; set; } = string.Empty;
        public string ErrorMessage      { get; set; } = string.Empty;
        public bool   IsPaid            { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // NSDL RESPONSE — parsed from pipe-delimited POST from NSDL gateway
    // ══════════════════════════════════════════════════════════════════════════

    public class NsdlResponseModel
    {
        public string Msg { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BILLDESK RESPONSE — JWT-decoded from BillDesk gateway POST
    // ══════════════════════════════════════════════════════════════════════════

    public class BillDeskResponseModel
    {
        public string transaction_response { get; set; } = string.Empty;
    }

    // Decoded BillDesk transaction JSON structure
    public class BillDeskTransactionResponse
    {
        public string mercid                 { get; set; } = string.Empty;
        public string transaction_date       { get; set; } = string.Empty;
        public string surcharge              { get; set; } = string.Empty;
        public string payment_method_type    { get; set; } = string.Empty;
        public string amount                 { get; set; } = string.Empty;
        public string ru                     { get; set; } = string.Empty;
        public string orderid                { get; set; } = string.Empty;
        public string transaction_error_type { get; set; } = string.Empty;
        public string discount               { get; set; } = string.Empty;
        public string transactionid          { get; set; } = string.Empty;
        public string txn_process_type       { get; set; } = string.Empty;
        public string bankid                 { get; set; } = string.Empty;
        public string itemcode               { get; set; } = string.Empty;
        public string transaction_error_code { get; set; } = string.Empty;
        public string transaction_error_desc { get; set; } = string.Empty;
        public string currency               { get; set; } = string.Empty;
        public string auth_status            { get; set; } = string.Empty;
        public string objectid               { get; set; } = string.Empty;
        public string charge_amount          { get; set; } = string.Empty;
        public BillDeskAdditionalInfo? additional_info { get; set; }
    }

    public class BillDeskAdditionalInfo
    {
        public string additional_info1  { get; set; } = string.Empty;
        public string additional_info2  { get; set; } = string.Empty;
        public string additional_info3  { get; set; } = string.Empty;
        public string additional_info4  { get; set; } = string.Empty;
        public string additional_info5  { get; set; } = string.Empty;
        public string additional_info6  { get; set; } = string.Empty;
        public string additional_info7  { get; set; } = string.Empty;
        public string additional_info8  { get; set; } = string.Empty;
        public string additional_info9  { get; set; } = string.Empty;
        public string additional_info10 { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PAYMENT SUCCESS / FAILED page query params
    // ══════════════════════════════════════════════════════════════════════════

    public class PaymentSuccessInfo
    {
        public bool   Success         { get; set; }
        public string Message         { get; set; } = string.Empty;
        public long   TransactionID   { get; set; }
        public string BankReferenceNo { get; set; } = string.Empty;
        public string FeeAmount       { get; set; } = string.Empty;
        public string RedirectUrl     { get; set; } = string.Empty;
    }

    public class PaymentFailedInfo
    {
        public bool   Success       { get; set; }
        public string Message       { get; set; } = string.Empty;
        public string FailedMessage { get; set; } = string.Empty;
        public string RedirectUrl   { get; set; } = string.Empty;
    }
}
