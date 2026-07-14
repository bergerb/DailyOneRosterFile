namespace DailyOneRosterFile.Api.Models;

public sealed class FileVariantOptions
{
    public const string SectionName = "FileVariant";

    public int SmallSchoolCount { get; set; } = 3;
    public int LargeSchoolCount { get; set; } = 22;
}
