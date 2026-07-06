using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DailyOneRosterFile.Api.Services;

public class TokenService(IOptions<StorageOptions> storageOptions) : ITokenService
{
    private readonly string _secret = storageOptions.Value.TokenSecret;

    public string GenerateToken(string fileName)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var payload = $"{fileName}.{expiry}";
        var signature = ComputeSignature(payload);
        var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}.{signature}";
        return token;
    }

    public bool ValidateToken(string token, string fileName)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return false;

            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var signature = parts[1];

            var payloadParts = payload.Split('.');
            if (payloadParts.Length != 2 || payloadParts[0] != fileName) return false;

            var expiry = long.Parse(payloadParts[1]);
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry) return false;

            var expectedSignature = ComputeSignature(payload);
            return signature == expectedSignature;
        }
        catch
        {
            return false;
        }
    }

    private string ComputeSignature(string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
