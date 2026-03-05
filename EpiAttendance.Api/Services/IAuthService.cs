using EpiAttendance.Api.DTOs;

namespace EpiAttendance.Api.Services;

public interface IAuthService
{
    Task<(bool Success, string? Message, AuthResponseDTO? Response)> RegisterAsync(RegisterRequestDTO dto);
    Task<(bool Success, string? Message, AuthResponseDTO? Response)> LoginAsync(LoginRequestDTO dto);
    Task<UserProfileDTO?> GetUserProfileAsync(string userId);
}