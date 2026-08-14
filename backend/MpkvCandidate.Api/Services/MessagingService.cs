using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace MpkvCandidate.Api.Services
{
    /// <summary>
    /// Handles SMS (MSG91) and Email (Gmail SMTP) sending.
    ///
    /// Mirrors old project exactly:
    ///   SMS   → MessagingHelperMsg91.SendSMS()  — MSG91 Flow API
    ///   Email → GmailMailer.SendEMailResetPassword() / SendEMailOthers()
    ///           smtp.gmail.com:587 with app passwords
    ///
    /// Purpose routing (same as old Mailer.SendEMail):
    ///   "NewCandidateRegistration" → noreply.mpkv@gmail.com
    ///   "ResetPassword"            → donotreply.mpkv1@gmail.com
    ///   everything else            → donotreply.mpkv2@gmail.com
    /// </summary>
    public interface IMessagingService
    {
        Task<bool> SendSmsAsync(string mobileNo, string message, string templateId = "");
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string purpose = "");
    }

    public class MessagingService : IMessagingService
    {
        private readonly IConfiguration           _config;
        private readonly ILogger<MessagingService> _logger;

        // MSG91 Flow API endpoint
        private const string Msg91ApiUrl = "https://api.msg91.com/api/v5/flow/";

        public MessagingService(IConfiguration config, ILogger<MessagingService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── SMS via MSG91 ─────────────────────────────────────────────────────
        // Mirrors: MessagingHelperMsg91.SendSMS(SMSEntity entity)
        // entity.Mobiles = "91" + MobileNo  (same prefix as old code)
        // entity.Var1    = OTP
        // entity.TemplateID = from Base_GetEMailSMS
        public async Task<bool> SendSmsAsync(string mobileNo, string message, string templateId = "")
        {
            try
            {
                var authKey  = _config["Messaging:Msg91AuthKey"] ?? "";
                var senderId = _config["Messaging:Msg91SenderID"] ?? "MPKVRH";

                if (string.IsNullOrEmpty(authKey))
                {
                    _logger.LogWarning("MSG91 AuthKey not configured.");
                    return false;
                }

                // MSG91 Flow API — same structure as old MessagingHelperMsg91
                var payload = new
                {
                    flow_id = templateId,
                    sender  = senderId,
                    mobiles = "91" + mobileNo.Trim(),   // "91" prefix — same as old entity.Mobiles
                    var1    = message,                   // OTP — same as old entity.Var1
                };

                using var client  = new HttpClient();
                client.DefaultRequestHeaders.Add("authkey", authKey);

                var json     = JsonSerializer.Serialize(payload);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(Msg91ApiUrl, content);
                var body     = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"MSG91 SMS to {mobileNo}: {response.StatusCode} — {body}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendSmsAsync error: {ex.Message}");
                return false;
            }
        }

        // ── Email via Gmail SMTP ──────────────────────────────────────────────
        // Mirrors: GmailMailer.SendEMailResetPassword / SendEMailOthers
        // smtp.gmail.com:587, EnableSsl=true, NetworkCredential with app password
        //
        // Purpose routing (same as old Mailer.SendEMail switch):
        //   "NewCandidateRegistration" → noreply.mpkv@gmail.com
        //   "ResetPassword"            → donotreply.mpkv1@gmail.com
        //   everything else            → donotreply.mpkv2@gmail.com
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string purpose = "")
        {
            return await Task.Run(() =>
            {
                try
                {
                    var smtpHost = _config["Messaging:Gmail:SmtpHost"] ?? "smtp.gmail.com";
                    var smtpPort = int.Parse(_config["Messaging:Gmail:SmtpPort"] ?? "587");
                    var fromName = _config["Messaging:Gmail:FromName"] ?? "MPKV, Rahuri";

                    // Select from address + password based on purpose — same as old switch
                    string fromAddress, password;

                    if (purpose == "NewCandidateRegistration")
                    {
                        fromAddress = _config["Messaging:Gmail:RegistrationFrom"]     ?? "";
                        password    = _config["Messaging:Gmail:RegistrationPassword"] ?? "";
                    }
                    else if (purpose == "ResetPassword")
                    {
                        fromAddress = _config["Messaging:Gmail:ResetPasswordFrom"]     ?? "";
                        password    = _config["Messaging:Gmail:ResetPasswordPassword"] ?? "";
                    }
                    else
                    {
                        fromAddress = _config["Messaging:Gmail:OthersFrom"]     ?? "";
                        password    = _config["Messaging:Gmail:OthersPassword"] ?? "";
                    }

                    if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(password))
                    {
                        _logger.LogWarning($"Gmail credentials not configured for purpose: {purpose}");
                        return false;
                    }

                    if (string.IsNullOrEmpty(toEmail))
                    {
                        _logger.LogWarning("SendEmailAsync: toEmail is empty.");
                        return false;
                    }

                    // Build SMTP client — mirrors old GmailMailer exactly
                    using var smtpClient = new SmtpClient(smtpHost, smtpPort);
                    smtpClient.EnableSsl             = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials           = new NetworkCredential(fromAddress, password);

                    using var mail = new MailMessage();
                    mail.From       = new MailAddress(fromAddress, fromName);
                    mail.To.Add(toEmail);
                    mail.Subject    = subject;
                    mail.Body       = htmlBody;
                    mail.IsBodyHtml = true;

                    smtpClient.Send(mail);

                    _logger.LogInformation($"Gmail SMTP email sent to {toEmail} for purpose: {purpose}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SendEmailAsync error (purpose={purpose}): {ex.Message}");
                    return false;
                }
            });
        }
    }
}
