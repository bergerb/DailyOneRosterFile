namespace DailyOneRosterFile.Api.Interfaces;

public interface IOneRosterFileGenerator
{
    Task GenerateDailyFileAsync(int schoolCount);
}
