using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EpiAttendance.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    public static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string password,
        string firstName = "Test",
        string lastName = "User") =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            confirmPassword = password,
            firstName,
            lastName
        });

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await LoginRawAsync(client, email, password);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    public static Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static HttpClient GetAuthorizedClient(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
