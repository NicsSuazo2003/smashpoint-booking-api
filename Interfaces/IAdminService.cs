using SmashPointBookingAPI.DTOs;

namespace SmashPointBookingAPI.Interfaces;

public interface IAdminService
{
    // ✅ UPDATED: Added optional startDate and endDate
    Task<AnalyticsDto> GetAnalyticsAsync(Guid clientId, DateTime? startDate = null, DateTime? endDate = null);

    Task<List<CourtAnalyticsDto>> GetCourtAnalyticsAsync(Guid clientId);
    Task<List<UserDto>> GetStaffByClientAsync(Guid clientId);
    Task<UserDto> CreateStaffAsync(CreateStaffRequest request, Guid clientId);
    Task UpdateStaffStatusAsync(Guid userId, string status, Guid clientId);
    Task DeleteStaffAsync(Guid userId, Guid clientId);
}