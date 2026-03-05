using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EpiAttendance.Api.Configuration;
using EpiAttendance.Api.DTOs;
using EpiAttendance.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace EpiAttendance.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }
    public async Task<(bool Success, string? Message, AuthResponseDTO? Response)> RegisterAsync(RegisterRequestDTO dto)
    {
        var existingUser = _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return (false, "User with this email already exists", null);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow
        };
        
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            return (false, errors, null);
        }

        var token = GenerateJwtToken(user);
        var response = new AuthResponseDTO
        {
            Token = token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes)
        };
        
        return (true, "Registration Successful", response);
    }

    public async Task<(bool Success, string? Message, AuthResponseDTO? Response)> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (false, "Invalid username or password", null);
        
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            return (false, "Invalid username or password", null);

        var token = GenerateJwtToken(user);
        var response = new AuthResponseDTO
        {
            Token = token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes)
        };

        return (true, "Login successful", response);
    }

    public async Task<UserProfileDTO?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        return new UserProfileDTO()
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
        };
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
            );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}