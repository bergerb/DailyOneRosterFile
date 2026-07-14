using DailyOneRosterFile.Api.Models;

namespace DailyOneRosterFile.Api.Tests.Models;

public class FileVariantOptionsTests
{
    [Fact]
    public void DefaultSmallSchoolCount_Is3()
    {
        var options = new FileVariantOptions();
        Assert.Equal(3, options.SmallSchoolCount);
    }

    [Fact]
    public void DefaultLargeSchoolCount_Is22()
    {
        var options = new FileVariantOptions();
        Assert.Equal(22, options.LargeSchoolCount);
    }

    [Fact]
    public void SectionName_IsFileVariant()
    {
        Assert.Equal("FileVariant", FileVariantOptions.SectionName);
    }
}
