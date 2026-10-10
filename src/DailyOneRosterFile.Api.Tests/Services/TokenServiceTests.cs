using System.Security.Cryptography;
using System.Text;
using DailyOneRosterFile.Api.Models;
using DailyOneRosterFile.Api.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace DailyOneRosterFile.Api.Tests.Services;

public class TokenServiceTests
{
    private const string Secret = "test-secret";

    private static TokenService CreateService(string secret = Secret) =>
        new(Options.Create(new StorageOptions { TokenSecret = secret }));

    private static string Sign(string payload, string secret = Secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void GenerateToken_ThenValidateToken_WithDottedFileName_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = service.GenerateToken("OneRoster.zip");
        var isValid = service.ValidateToken(token, "OneRoster.zip");

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var expiredPayload = $"OneRoster.zip.{DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds()}";
        var token = $"{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(expiredPayload))}.{Sign(expiredPayload)}";

        // Act
        var isValid = service.ValidateToken(token, "OneRoster.zip");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_TamperedSignature_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var token = service.GenerateToken("OneRoster.zip");
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.{Convert.ToBase64String(Encoding.UTF8.GetBytes("tampered"))}";

        // Act
        var isValid = service.ValidateToken(tamperedToken, "OneRoster.zip");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_WrongFileName_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var token = service.GenerateToken("OneRoster.zip");

        // Act
        var isValid = service.ValidateToken(token, "Other.zip");

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("onlyonepart")]
    public void ValidateToken_MalformedToken_ReturnsFalse(string token)
    {
        // Arrange
        var service = CreateService();

        // Act
        var isValid = service.ValidateToken(token, "OneRoster.zip");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GenerateToken_ProducesUrlSafeToken_WithoutQueryStringSpecialCharacters()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = service.GenerateToken("OneRoster.zip");

        // Assert
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void ValidateToken_AfterQueryStringRoundTrip_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var token = service.GenerateToken("OneRoster.zip");

        // Act
        var parsed = QueryHelpers.ParseQuery($"?token={token}&variant=large")["token"].ToString();
        var isValid = service.ValidateToken(parsed, "OneRoster.zip");

        // Assert
        Assert.Equal(token, parsed);
        Assert.True(isValid);
    }
}
