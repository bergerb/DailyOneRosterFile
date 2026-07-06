namespace DailyOneRosterFile.Api.Interfaces;

public interface ITokenService
{
    string GenerateToken(string fileName);
    bool ValidateToken(string token, string fileName);
}
