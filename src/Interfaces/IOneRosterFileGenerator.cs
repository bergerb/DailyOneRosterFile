namespace DailyOneRosterFile.Api.Interfaces;

public interface IOneRosterFileGenerator
{
    Task<string> GenerateDailyFileAsync();
}
