using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmashPointBookingAPI.Data;
using SmashPointBookingAPI.DTOs;
using SmashPointBookingAPI.Interfaces;
using SmashPointBookingAPI.Middleware;

namespace SmashPointBookingAPI.Controllers;

[ApiController, Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IBookingService _booking;
    private readonly ICourtService _court;
    private readonly ClientResolver _clientResolver;
    private readonly IClientService _clientService;
    private readonly IPricingRuleService _pricingRuleService;
    private readonly AppDbContext _db;

    public AdminController(
        IAdminService admin,
        IBookingService booking,
        ICourtService court,
        ClientResolver clientResolver,
        IClientService clientService,
        IPricingRuleService pricingRuleService,
        AppDbContext db)
    {
        _admin = admin;
        _booking = booking;
        _court = court;
        _clientResolver = clientResolver;
        _clientService = clientService;
        _pricingRuleService = pricingRuleService;
        _db = db;
    }

    /// <summary>
    /// Resolves the effective client for this request AND (🔒 Fix D)
    /// verifies it matches the authenticated user's tenant claim.
    /// </summary>
    private async Task<Guid> GetClientId()
    {
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            throw new UnauthorizedAccessException("Client identification required");

        var resolved = await _clientService.GetClientIdBySubdomainAsync(subdomain);

        if (User?.Identity?.IsAuthenticated == true)
        {
            var claimRaw = User.FindFirstValue("client_id");
            if (!Guid.TryParse(claimRaw, out var jwtClientId) || jwtClientId != resolved)
            {
                throw new UnauthorizedAccessException("Client identification required");
            }
        }

        return resolved;
    }

    // ========================================
    // STAFF & ADMIN ACCESS
    // ========================================

    [HttpGet("bookings")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<List<BookingDto>>> GetBookings()
    {
        var clientId = await GetClientId();
        var bookings = await _booking.GetAllBookingsAsync(clientId);
        return Ok(bookings);
    }

    [HttpPut("bookings/{id}")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<BookingDto>> UpdateBooking(Guid id, AdminUpdateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.AdminUpdateBookingAsync(id, request, clientId);
        return Ok(booking);
    }

    [HttpPost("bookings/manual")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<BookingDto>> CreateManualBooking(StaffCreateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.CreateStaffBookingAsync(request, clientId);
        return Ok(booking);
    }

    [HttpPatch("bookings/{id}/reschedule")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<RescheduleBookingResponse>> RescheduleBooking(
        Guid id,
        RescheduleBookingRequest request)
    {
        var clientId = await GetClientId();
        var result = await _booking.RescheduleBookingAsync(id, request, clientId);
        return Ok(result);
    }

    // ========================================
    // ADMIN ONLY ACCESS
    // ========================================

    [HttpGet("analytics")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var clientId = await GetClientId();
        var analytics = await _admin.GetAnalyticsAsync(clientId, startDate, endDate);
        return Ok(analytics);
    }

    [HttpGet("analytics/courts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<CourtAnalyticsDto>>> GetCourtAnalytics()
    {
        var clientId = await GetClientId();
        var analytics = await _admin.GetCourtAnalyticsAsync(clientId);
        return Ok(analytics);
    }

    [HttpGet("courts")]
    [Authorize(Roles = "admin,staff")]
    public async Task<ActionResult<List<CourtDto>>> GetCourts()
    {
        var clientId = await GetClientId();
        var courts = await _court.GetAllCourtsAsync(clientId);
        return Ok(courts);
    }

    [HttpGet("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> GetCourt(Guid id)
    {
        var clientId = await GetClientId();
        var court = await _court.GetCourtByIdAsync(id, clientId);
        return Ok(court);
    }

    [HttpPost("courts")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> CreateCourt(CreateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.CreateCourtAsync(request, clientId);
        return CreatedAtAction(nameof(GetCourt), new { id = court.Id }, court);
    }

    [HttpPut("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourtDto>> UpdateCourt(Guid id, UpdateCourtRequest request)
    {
        var clientId = await GetClientId();
        var court = await _court.UpdateCourtAsync(id, request, clientId);
        return Ok(court);
    }

    [HttpDelete("courts/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteCourt(Guid id)
    {
        var clientId = await GetClientId();
        await _court.DeleteCourtAsync(id, clientId);
        return NoContent();
    }

    [HttpGet("settings")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ClientDto>> GetSettings()
    {
        // 🔒 Fix D: tenant claim guard also applies here (this endpoint bypasses GetClientId).
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            throw new UnauthorizedAccessException("Client identification required");

        var resolved = await _clientService.GetClientIdBySubdomainAsync(subdomain);

        var claimRaw = User.FindFirstValue("client_id");
        if (!Guid.TryParse(claimRaw, out var jwtClientId) || jwtClientId != resolved)
            throw new UnauthorizedAccessException("Client identification required");

        var client = await _clientService.GetClientBySubdomainAsync(subdomain);
        return Ok(client);
    }

    [HttpPut("settings")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ClientDto>> UpdateSettings(UpdateClientSettingsRequest request)
    {
        var clientId = await GetClientId();
        var client = await _clientService.UpdateClientSettingsAsync(clientId, request);
        return Ok(client);
    }

    // ========================================
    // STAFF MANAGEMENT
    // ========================================

    [HttpGet("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UserDto>>> GetStaff()
    {
        var clientId = await GetClientId();
        var users = await _admin.GetStaffByClientAsync(clientId);
        return Ok(users);
    }

    [HttpPost("staff")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserDto>> CreateStaff(CreateStaffRequest request)
    {
        var clientId = await GetClientId();
        var user = await _admin.CreateStaffAsync(request, clientId);
        return Ok(user);
    }

    [HttpPut("staff/{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> UpdateStaffStatus(Guid id, UpdateStaffStatusRequest request)
    {
        var clientId = await GetClientId();
        await _admin.UpdateStaffStatusAsync(id, request.Status, clientId);
        return Ok(new { message = "Staff status updated" });
    }

    [HttpDelete("staff/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> DeleteStaff(Guid id)
    {
        var clientId = await GetClientId();
        await _admin.DeleteStaffAsync(id, clientId);
        return Ok(new { message = "Staff removed" });
    }

    // ========================================
    // PRICING RULES
    // ========================================

    [HttpGet("courts/{courtId}/pricing-rules")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<PricingRuleDto>>> GetPricingRules(Guid courtId)
    {
        var clientId = await GetClientId();
        var rules = await _pricingRuleService.GetRulesAsync(courtId, clientId);
        return Ok(rules);
    }

    [HttpPost("courts/{courtId}/pricing-rules")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PricingRuleDto>> CreatePricingRule(
        Guid courtId, CreatePricingRuleRequest request)
    {
        var clientId = await GetClientId();
        var rule = await _pricingRuleService.CreateRuleAsync(courtId, clientId, request);
        return Ok(rule);
    }

    [HttpPut("pricing-rules/{ruleId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PricingRuleDto>> UpdatePricingRule(
        Guid ruleId, UpdatePricingRuleRequest request)
    {
        var clientId = await GetClientId();
        var rule = await _pricingRuleService.UpdateRuleAsync(ruleId, clientId, request);
        return Ok(rule);
    }

    [HttpDelete("pricing-rules/{ruleId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeletePricingRule(Guid ruleId)
    {
        var clientId = await GetClientId();
        await _pricingRuleService.DeleteRuleAsync(ruleId, clientId);
        return NoContent();
    }

    // ========================================
    // SECURITY EVENTS
    // ========================================

    [HttpGet("security-events")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<SecurityEventDto>>> GetSecurityEvents(
        [FromQuery] string? severity,
        [FromQuery] string? eventType,
        [FromQuery] int limit = 100)
    {
        var clientId = await GetClientId();

        var q = _db.SecurityEvents
            .Where(e => e.ClientId == clientId)
            .OrderByDescending(e => e.OccurredAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(severity))
            q = q.Where(e => e.Severity == severity);
        if (!string.IsNullOrWhiteSpace(eventType))
            q = q.Where(e => e.EventType == eventType);

        var events = await q
            .Take(Math.Min(limit, 500))
            .Select(e => new SecurityEventDto(
                e.Id.ToString(),
                e.OccurredAt,
                e.EventType,
                e.Severity,
                e.Message,
                e.ActorEmail,
                e.IpHash,
                e.Resource,
                e.Success,
                e.ErrorCode))
            .ToListAsync();

        return Ok(events);
    }
}