using System.Net;
using System.Net.Http.Json;
using EpiAttendance.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace EpiAttendance.IntegrationTests.Auth;

[Collection("Integration")]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_Returns200()
    {
        var email = $"{Guid.NewGuid()}@test.com";

        var response = await AuthHelper.RegisterAsync(_client, email, "Password1!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email, "Password1!");

        var response = await AuthHelper.RegisterAsync(_client, email, "Password1!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var email = $"{Guid.NewGuid()}@test.com";

        var response = await AuthHelper.RegisterAsync(_client, email, "weak");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email, "Password1!");

        var token = await AuthHelper.LoginAsync(_client, email, "Password1!");

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email, "Password1!");

        var response = await AuthHelper.LoginRawAsync(_client, email, "WrongPassword!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AccountLockedOut_Returns401()
    {
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email, "Password1!");

        // Trigger lockout (5 failed attempts)
        for (var i = 0; i < 5; i++)
            await AuthHelper.LoginRawAsync(_client, email, "WrongPassword!");

        var response = await AuthHelper.LoginRawAsync(_client, email, "Password1!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_WithValidToken_Returns200()
    {
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(_client, email, "Password1!");
        var token = await AuthHelper.LoginAsync(_client, email, "Password1!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/profile");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Profile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
