namespace DailyOneRosterFile.Api.Models;

public static class FileVariant
{
    public const string Small = "small";
    public const string Large = "large";
    private const int SmallSchoolThreshold = 5;

    public static string GetFolder(int schoolCount) =>
        schoolCount <= SmallSchoolThreshold ? Small : Large;
}
