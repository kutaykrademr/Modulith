using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Modulith.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns_Healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Register_Returns_201_And_Duplicate_Returns_409()
    {
        var payload = new { email = UniqueEmail(), fullName = "New User", password = "Passw0rd!123" };

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Register_With_Weak_Password_Returns_422()
    {
        var payload = new { email = UniqueEmail(), fullName = "Weak Pwd", password = "short" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Login_Before_Email_Verified_Returns_401()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, fullName = "Unverified", password = "Passw0rd!123" });

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = "Passw0rd!123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_Nonexistent_User_Returns_401()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = UniqueEmail(), password = "Passw0rd!123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Profile_Without_Token_Returns_401()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
