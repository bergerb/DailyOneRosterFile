namespace DailyOneRosterFile.Api.Interfaces;

public interface IOneRosterFileGenerator
{
    Task GenerateDailyFileAsync(string variant, int schoolCount);
}
