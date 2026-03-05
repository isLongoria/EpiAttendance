using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EpiAttendance.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace EpiAttendance.IntegrationTests.Attendance;

[Collection("Integration")]
public class AttendanceControllerTests
{
    private readonly ApiFactory _factory;

    public AttendanceControllerTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        await AuthHelper.RegisterAsync(client, email, "Password1!");
        var token = await AuthHelper.LoginAsync(client, email, "Password1!");
        AuthHelper.GetAuthorizedClient(client, token);
        return client;
    }

    [Fact]
    public async Task GetByDate_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/attendance/date/2026-01-15");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRecord_ValidData_Returns200WithCorrectData()
    {
        var client = await CreateAuthenticatedClientAsync();
        var date = $"2026-{Guid.NewGuid().GetHashCode() % 12 + 1:D2}-15";

        var response = await client.PostAsJsonAsync("/api/attendance", new
        {
            date,
            type = 1 // Attended
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("type").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetByDate_ExistingRecord_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var date = new DateOnly(2026, 3, 10);

        await client.PostAsJsonAsync("/api/attendance", new { date = date.ToString("yyyy-MM-dd"), type = 1 });

        var response = await client.GetAsync($"/api/attendance/date/{date:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByDate_NoRecord_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/attendance/date/2025-06-15");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByMonth_Returns200WithList()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/attendance", new { date = "2026-04-01", type = 1 });
        await client.PostAsJsonAsync("/api/attendance", new { date = "2026-04-02", type = 1 });

        var response = await client.GetAsync("/api/attendance/month/2026/4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSummary_Returns200WithCorrectCounts()
    {
        var client = await CreateAuthenticatedClientAsync();
        for (var day = 1; day <= 12; day++)
            await client.PostAsJsonAsync("/api/attendance", new { date = $"2026-05-{day:D2}", type = 1 });

        var response = await client.GetAsync("/api/attendance/summary/2026/5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("totalAttendedDays").GetInt32().Should().BeGreaterThanOrEqualTo(12);
        json.GetProperty("requirementMet").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetCount_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/attendance", new { date = "2026-06-01", type = 1 });

        var response = await client.GetAsync("/api/attendance/count/2026/6");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("attendedDays").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task UpdateRecord_ExistingDate_Returns200WithUpdatedFields()
    {
        var client = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/attendance", new { date = "2026-07-01", type = 1 });

        var response = await client.PostAsJsonAsync("/api/attendance", new
        {
            date = "2026-07-01",
            type = 2, // PTO
            notes = "Vacation"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("type").GetInt32().Should().Be(2);
        json.GetProperty("notes").GetString().Should().Be("Vacation");
    }

    [Fact]
    public async Task DeleteRecord_Returns204ThenSubsequentDeleteReturns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/attendance", new { date = "2026-08-01", type = 1 });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var deleteResponse = await client.DeleteAsync($"/api/attendance/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondDeleteResponse = await client.DeleteAsync($"/api/attendance/{id}");
        secondDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UserScoping_UserBCannotDeleteUserARecord()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        var createResponse = await clientA.PostAsJsonAsync("/api/attendance", new { date = "2026-09-01", type = 1 });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var response = await clientB.DeleteAsync($"/api/attendance/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UserScoping_UserBDoesNotSeeUserARecordOnSameDate()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var date = "2026-10-15";

        await clientA.PostAsJsonAsync("/api/attendance", new { date, type = 1 });

        var response = await clientB.GetAsync($"/api/attendance/date/{date}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAttendance_NotesExceeding500Chars_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var longNotes = new string('x', 501);

        var response = await client.PostAsJsonAsync("/api/attendance", new
        {
            date = "2026-11-01",
            type = 1,
            notes = longNotes
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
