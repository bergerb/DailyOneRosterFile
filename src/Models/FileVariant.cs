namespace DailyOneRosterFile.Api.Models;

public static class FileVariant
{
    public const string Small = "small";
    public const string Large = "large";
    public const int SmallSchoolCount = 3;
    public const int LargeSchoolCount = 22;

    public static string GetFolder(int schoolCount) =>
        schoolCount <= SmallSchoolCount ? Small : Large;
}
