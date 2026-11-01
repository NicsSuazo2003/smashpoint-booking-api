using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmashPointBookingAPI.Entities;

public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#1A2E1A";
    public string AccentColor { get; set; } = "#C9A94E";
    public string? GcashNumber { get; set; }
    public string? GcashAccountName { get; set; }

    [Column(TypeName = "jsonb")]
    public string? PaymentMethods { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AvailableAmenitiesJson { get; set; }

    [NotMapped]
    public List<AmenityItem> AvailableAmenities
    {
        get => string.IsNullOrEmpty(AvailableAmenitiesJson)
            ? new List<AmenityItem>()
            : JsonSerializer.Deserialize<List<AmenityItem>>(AvailableAmenitiesJson)
              ?? new List<AmenityItem>();
        set => AvailableAmenitiesJson = JsonSerializer.Serialize(value);
    }

    public int MaxAdvanceBookingDays { get; set; } = 90;

    [Column("bookings_enabled")]
    public bool BookingsEnabled { get; set; } = true;

    // ── Booking window mode ──
    // "rolling" → MaxAdvanceBookingDays controls the far edge
    // "fixed"   → [BookingOpenDate .. BookingCloseDate] inclusive
    // "closed"  → no customer-facing bookings (staff unaffected)
    public string BookingWindowMode { get; set; } = "rolling";

    private DateTime? _bookingOpenDate;
    public DateTime? BookingOpenDate
    {
        get => _bookingOpenDate;
        set => _bookingOpenDate = value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;
    }

    private DateTime? _bookingCloseDate;
    public DateTime? BookingCloseDate
    {
        get => _bookingCloseDate;
        set => _bookingCloseDate = value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public string Status { get; set; } = "active";

    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class AmenityItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "Sparkles";
    public string? Description { get; set; }
}