using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace SmashPointBookingAPI.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    // ── Neutrals — Carbon Pitch ──
    private const string OUTER_BG = "#0A0B0D";
    private const string BODY_BG = "#111317";
    private const string BANNER_BG = "#1D2028";
    private const string DIVIDER = "#313642";

    // ── Primary — Championship Red ──
    private const string PRIMARY = "#ED1C24";
    private const string PRIMARY_TEXT = "#FFFFFF";

    // ── Accent — Optic Court Gold ──
    private const string ACCENT = "#FFB703";

    // ── Cream / Silver White ──
    private const string TEXT_CREAM = "#FFFFFF";
    private const string TEXT_CREAM_DARK = "#EEF2F6";
    private const string TEXT_CREAM_MUTED = "#7A8395";

    // ── Semantic ──
    private const string SUCCESS = "#10B981";
    private const string ERROR = "#ED1C24";
    private const string WARNING = "#FFB703";
    private const string INFO = "#38BDF8";

    private const string ADMIN_URL = "https://centercourt-booking.vercel.app/admin";

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ? Time formatting
    private static string FormatTime(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        input = input.Trim();

        if (Regex.IsMatch(input, @"\b(AM|PM)\b", RegexOptions.IgnoreCase))
            return input;

        var match = Regex.Match(input, @"^(\d{1,2}):(\d{2})");
        if (!match.Success) return input;

        if (!int.TryParse(match.Groups[1].Value, out var hour)) return input;
        var minute = match.Groups[2].Value;

        var period = hour >= 12 ? "PM" : "AM";
        var hour12 = hour % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12}:{minute} {period}";
    }

    private static string FormatTimeRange(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        // ?? Handle multi-court strings like "Court 1: 07:00-08:00 | Court 2: 07:00-08:00"
        if (input.Contains('|'))
        {
            return string.Join(" | ", input.Split('|').Select(part =>
            {
                var trimmed = part.Trim();
                // Match "Court Name" + ":" + "HH:mm-HH:mm"
                var match = Regex.Match(trimmed, @"^(.*?):\s*(\d{1,2}:\d{2}.*)$");

                if (match.Success)
                {
                    var courtName = match.Groups[1].Value.Trim();
                    var timeRange = match.Groups[2].Value.Trim();
                    var parts = Regex.Split(timeRange, @"\s*[-–—]\s*");

                    if (parts.Length == 2)
                    {
                        return $"{courtName}: {FormatTime(parts[0])} – {FormatTime(parts[1])}";
                    }
                }
                return trimmed;
            }));
        }

        // ?? Handle single court string like "Court 3: 22:00-23:00"
        var singleMatch = Regex.Match(input.Trim(), @"^(.*?):\s*(\d{1,2}:\d{2}.*)$");
        if (singleMatch.Success)
        {
            var courtName = singleMatch.Groups[1].Value.Trim();
            var timeRange = singleMatch.Groups[2].Value.Trim();
            var parts = Regex.Split(timeRange, @"\s*[-–—]\s*");

            if (parts.Length == 2)
            {
                return $"{courtName}: {FormatTime(parts[0])} – {FormatTime(parts[1])}";
            }
        }

        // Fallback for just time ranges without a court name
        var singleParts = Regex.Split(input.Trim(), @"\s*[-–—]\s*");
        if (singleParts.Length == 2)
        {
            return $"{FormatTime(singleParts[0])} – {FormatTime(singleParts[1])}";
        }

        return FormatTime(input);
    }

    private static string FormatDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        if (DateTime.TryParse(input, out var dt))
        {
            return dt.ToString("MMMM d, yyyy");
        }

        return input;
    }

    // ?? Shared layout — Smash Point
    private static string WrapLayout(string bannerTitle, string contentHtml)
    {
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<meta name='color-scheme' content='dark light'>
<title>Smash Point</title>
</head>
<body style='margin:0;padding:0;background-color:{OUTER_BG};font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:{OUTER_BG};padding:24px 12px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:560px;background-color:{BODY_BG};border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.55),0 0 0 1px rgba(255,255,255,0.04);'>
          
          <!-- Banner Header -->
          <tr>
            <td style='background-color:{BANNER_BG};border-bottom:2px solid {PRIMARY};padding:26px 32px 22px;text-align:left;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:2.5px;color:{ACCENT};text-transform:uppercase;margin-bottom:6px;'>
                SMASH POINT
              </div>
              <div style='font-size:24px;font-weight:800;color:{TEXT_CREAM};line-height:1.2;margin:0;letter-spacing:-0.3px;'>
                {bannerTitle}
              </div>
            </td>
          </tr>

          <!-- Body Content -->
          <tr>
            <td style='padding:32px;'>
              {contentHtml}
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='padding:20px 32px 26px;border-top:1px solid {DIVIDER};'>
              <div style='font-size:11px;color:{TEXT_CREAM_MUTED};text-align:center;line-height:1.6;'>
                Smash Point Pickle Sports Center · Tandag City<br>
                Book Your Court. Play Your Game.<br>
                Automated message — please do not reply directly.
              </div>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string KvRow(string label, string value, bool isLast = false)
    {
        var border = isLast ? "" : $"border-bottom:1px solid {DIVIDER};";
        return $@"
              <tr>
                <td style='padding:14px 0;{border}'>
                  <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>
                    {label}
                  </div>
                  <div style='font-size:16px;font-weight:600;color:{TEXT_CREAM};line-height:1.35;'>
                    {value}
                  </div>
                </td>
              </tr>";
    }

    private static string CtaButton(string url, string text)
    {
        return $@"
              <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin-top:28px;'>
                <tr>
                  <td align='center' style='border-radius:10px;background-color:{PRIMARY};box-shadow:0 0 20px -3px rgba(237,28,36,0.45);'>
                    <a href='{url}' target='_blank' style='display:inline-block;padding:13px 30px;font-size:14px;font-weight:700;color:{PRIMARY_TEXT};text-decoration:none;border-radius:10px;'>
                      {text}
                    </a>
                  </td>
                </tr>
              </table>";
    }

    private static string StatusChip(string text, string color)
    {
        return $@"
              <div style='display:inline-block;padding:6px 14px;border-radius:999px;background-color:{color}1A;border:1px solid {color}55;margin-bottom:24px;'>
                <span style='font-size:11px;font-weight:800;letter-spacing:1.5px;color:{color};text-transform:uppercase;'>— {text}</span>
              </div>";
    }

    // -------------------------------------------------------------
    // 1. ADMIN — New Booking
    // -------------------------------------------------------------
    public async Task NotifyAdminNewBookingAsync(string customerName, string referenceCode, string date, string time, string amount)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var adminEmail = _config["Brevo:AdminEmail"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var content = $@"
              <p style='margin:0 0 22px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                A new booking has come in. Review the details below and confirm once payment is verified.
              </p>

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Customer", customerName)}
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {KvRow("Amount", amount, isLast: true)}
              </table>

              {CtaButton(ADMIN_URL, "Review in Admin Panel")}
            ";

            var html = WrapLayout("New Booking", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, adminEmail, "Admin",
                $"New Booking: {referenceCode} — {customerName}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification failed");
        }
    }

    // -------------------------------------------------------------
    // 2. CUSTOMER — Booking Confirmed
    // -------------------------------------------------------------
    public async Task NotifyCustomerBookingConfirmedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_DARK};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                Great news — your booking has been <strong style='color:{SUCCESS};font-weight:700;'>confirmed</strong>. See you on the court!
              </p>

              {StatusChip("Paid", SUCCESS)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Confirmed", isLast: true)}
              </table>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Confirmed", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"Booking Confirmed: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer email notification failed");
        }
    }

    // -------------------------------------------------------------
    // 3. CUSTOMER — Booking Rejected
    // -------------------------------------------------------------
    public async Task NotifyCustomerBookingRejectedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? reason = null,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {ERROR};background-color:{ERROR}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_DARK};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                Unfortunately, your booking could not be approved at this time.
              </p>

              {StatusChip("Not Approved", ERROR)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Not Approved", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                If you believe this is a mistake, or would like to rebook, please get in touch — we're happy to help.
              </p>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Not Approved", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"Booking Not Approved: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rejection email notification failed");
        }
    }

    // -------------------------------------------------------------
    // 4. CUSTOMER — Booking Cancelled
    // -------------------------------------------------------------
    public async Task NotifyCustomerBookingCancelledAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? reason = null,
        string? amount = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Paid", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {WARNING};background-color:{WARNING}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_DARK};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                Your booking has been <strong style='color:{WARNING};font-weight:700;'>cancelled</strong>. If this was unexpected, please reach out and we'll help sort it out.
              </p>

              {StatusChip("Cancelled", WARNING)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Cancelled", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                Want to play again? You can start a fresh booking anytime.
              </p>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Cancelled", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"Booking Cancelled: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancellation email notification failed");
        }
    }

    // -------------------------------------------------------------
    // 5. CUSTOMER — Booking Refunded
    // -------------------------------------------------------------
    public async Task NotifyCustomerBookingRefundedAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string date,
        string time,
        string? amount = null,
        string? reason = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyDate = FormatDate(date);
            var prettyTime = FormatTimeRange(time);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("Amount Refunded", amount);

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {INFO};background-color:{INFO}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Reason</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_DARK};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                Your booking has been <strong style='color:{INFO};font-weight:700;'>refunded</strong>. The amount will be returned to your original payment method.
              </p>

              {StatusChip("Refunded", INFO)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Schedule", $"{prettyDate} · {prettyTime}")}
                {amountRow}
                {KvRow("Status", "Refunded", isLast: true)}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                Refunds are typically processed within 3–5 business days, depending on your bank or e-wallet. If you haven't received it by then, please reach out and we'll check on it.
              </p>

              <p style='margin:16px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                We'd love to see you back on the court sometime soon!
              </p>

              {CtaButton($"{frontendUrl}/", "Book Again")}
            ";

            var html = WrapLayout("Booking Refunded", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"Booking Refunded: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund email notification failed");
        }
    }

    // -------------------------------------------------------------
    // 6. CUSTOMER — Booking Rescheduled
    // -------------------------------------------------------------
    public async Task NotifyCustomerBookingRescheduledAsync(
        string customerEmail,
        string customerName,
        string referenceCode,
        string oldDate,
        string oldTime,
        string oldCourtName,
        string newDate,
        string newTime,
        string newCourtName,
        string? amount = null,
        string? reason = null)
    {
        try
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];
            var frontendUrl = _config["App:FrontendUrl"];

            var prettyOldDate = FormatDate(oldDate);
            var prettyOldTime = FormatTimeRange(oldTime);
            var prettyNewDate = FormatDate(newDate);
            var prettyNewTime = FormatTimeRange(newTime);

            var amountRow = string.IsNullOrWhiteSpace(amount)
                ? ""
                : KvRow("New Total", amount, isLast: string.IsNullOrWhiteSpace(reason));

            var reasonBlock = string.IsNullOrWhiteSpace(reason)
                ? ""
                : $@"
              <div style='margin-top:20px;padding:14px 16px;border-left:3px solid {ACCENT};background-color:{ACCENT}15;border-radius:6px;'>
                <div style='font-size:10px;font-weight:800;letter-spacing:1.8px;color:{TEXT_CREAM_MUTED};text-transform:uppercase;margin-bottom:5px;'>Note from the team</div>
                <div style='font-size:14px;color:{TEXT_CREAM};line-height:1.55;'>{reason}</div>
              </div>";

            var content = $@"
              <p style='margin:0 0 8px;font-size:15px;color:{TEXT_CREAM_DARK};'>
                Hi {customerName},
              </p>
              <p style='margin:0 0 24px;font-size:15px;line-height:1.65;color:{TEXT_CREAM_DARK};'>
                Your booking has been <strong style='color:{ACCENT};font-weight:700;'>moved</strong>. Please double-check the new schedule below.
              </p>

              {StatusChip("Rescheduled", ACCENT)}

              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                {KvRow("Reference", referenceCode)}
                {KvRow("Previous Schedule", $"{prettyOldDate} · {prettyOldTime}")}
                {KvRow("Previous Court", oldCourtName)}
                {KvRow("New Schedule", $"{prettyNewDate} · {prettyNewTime}")}
                {KvRow("New Court", newCourtName)}
                {amountRow}
              </table>

              {reasonBlock}

              <p style='margin:24px 0 0;font-size:14px;line-height:1.65;color:{TEXT_CREAM_MUTED};'>
                If the new schedule doesn't work for you, reply to this email or reach out and we'll sort it out.
              </p>

              {CtaButton($"{frontendUrl}/track", "Track Your Booking")}
            ";

            var html = WrapLayout("Booking Rescheduled", content);
            // REMOVED EMOJI FROM SUBJECT
            await SendAsync(apiKey, senderEmail, senderName, customerEmail, customerName,
                $"Booking Rescheduled: {referenceCode}", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reschedule email notification failed");
        }
    }

    // -------------------------------------------------------------
    // ?? Shared sender
    // -------------------------------------------------------------
    private async Task SendAsync(
        string? apiKey,
        string? senderEmail,
        string? senderName,
        string? toEmail,
        string toName,
        string subject,
        string html)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("Brevo config incomplete — skipping email {Subject}", subject);
            return;
        }

        using var http = new HttpClient();

        var payload = new
        {
            sender = new { email = senderEmail, name = senderName ?? "Smash Point" },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = html
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            // FORCE UTF-8 ENCODING SO THE PESO SYMBOL (₱) DOES NOT BECOME '?'
            Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("api-key", apiKey);

        var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Brevo failed: {Status} {Body}", response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation("Email sent successfully to {To} ({Subject})", toEmail, subject);
        }
    }
}