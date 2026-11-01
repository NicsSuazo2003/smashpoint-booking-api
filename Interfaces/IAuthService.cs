using SmashPointBookingAPI.DTOs;

namespace SmashPointBookingAPI.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        string? userAgent = null);

    Task<AuthResponse> RegisterAsync(RegisterRequest request, Guid clientId);
    Task<AuthResponse> RegisterStaffAsync(RegisterRequest request, Guid clientId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}