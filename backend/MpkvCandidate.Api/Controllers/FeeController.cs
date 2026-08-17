using Microsoft.AspNetCore.Mvc;
using MpkvCandidate.Api.Models;
using MpkvCandidate.Api.Services;

namespace MpkvCandidate.Api.Controllers
{
    // ══════════════════════════════════════════════════════════════════════════
    // FeeController — payment gateway callbacks (no JWT auth required)
    // These endpoints are called by the NSDL/BillDesk gateways directly
    // ══════════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/fee")]
    public class FeeController : ControllerBase
    {
        private readonly IFeeService    _feeService;
        private readonly IConfiguration _config;

        public FeeController(IFeeService feeService, IConfiguration config)
        {
            _feeService = feeService;
            _config     = config;
        }

        // Frontend base URL for redirects after payment
        private string FrontendBase =>
            _config["AllowedOrigins"]?.TrimEnd('/') ?? "http://localhost:5173";

        // ══════════════════════════════════════════════════════════════════════
        // POST /api/fee/nsdl-response
        // Called by NSDL gateway after payment (browser POST-back)
        // Mirrors: NsdlResponseHandler.aspx.cs
        // Receives Form["msg"] — pipe-delimited with CRC32 at end
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("nsdl-response")]
        public async Task<IActionResult> NsdlResponse([FromForm] string? msg)
        {
            var (isPaid, redirectUrl) = await _feeService.ProcessNsdlResponse(msg ?? "", FrontendBase);
            // Redirect browser to frontend success/failed page
            return Redirect(redirectUrl);
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST /api/fee/nsdl-push
        // Server-to-server push from NSDL (not browser)
        // Mirrors: NsdlPushResponseHandler.aspx.cs
        // Returns "200|Y" on success, "400|N" on failure
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("nsdl-push")]
        public async Task<IActionResult> NsdlPush([FromForm] string? msg)
        {
            var result = await _feeService.ProcessNsdlPushResponse(msg ?? "");
            return Content(result, "text/plain");
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST /api/fee/billdesk-response
        // Called by BillDesk gateway after payment (browser POST-back)
        // Mirrors: BillDeskResponseHandler.aspx.cs
        // Receives Form["transaction_response"] — JWS token
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("billdesk-response")]
        public async Task<IActionResult> BillDeskResponse([FromForm] string? transaction_response)
        {
            var (isPaid, redirectUrl) = await _feeService.ProcessBillDeskResponse(transaction_response ?? "", FrontendBase);
            return Redirect(redirectUrl);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET /api/fee/payment-success
        // Called from frontend PaymentSuccess page to verify + get details
        // Mirrors: PaymentSuccess.aspx.cs
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("payment-success")]
        public IActionResult PaymentSuccess([FromQuery] long txId, [FromQuery] string? refNo, [FromQuery] decimal amount)
        {
            if (txId <= 0)
                return BadRequest(new PaymentSuccessInfo { Success = false, Message = "Invalid transaction." });

            var txDetails = _feeService.GetTransactionDetails(txId);
            if (txDetails == null)
                return NotFound(new PaymentSuccessInfo { Success = false, Message = "Transaction not found." });

            return Ok(new PaymentSuccessInfo
            {
                Success         = true,
                Message         = "Payment successful.",
                TransactionID   = txId,
                BankReferenceNo = refNo ?? txDetails.BankRefereneceNo,
                FeeAmount       = amount > 0 ? amount.ToString("F2") : txDetails.FeeAmount.ToString("F2"),
                RedirectUrl     = "/candidate/summary"
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET /api/fee/payment-failed
        // Called from frontend PaymentFailed page to get failure details
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("payment-failed")]
        public IActionResult PaymentFailed([FromQuery] string? msg)
        {
            return Ok(new PaymentFailedInfo
            {
                Success       = false,
                Message       = "Payment failed or was cancelled.",
                FailedMessage = msg ?? "Your payment could not be processed. Please try again.",
                RedirectUrl   = "/candidate/fee"
            });
        }
    }
}
