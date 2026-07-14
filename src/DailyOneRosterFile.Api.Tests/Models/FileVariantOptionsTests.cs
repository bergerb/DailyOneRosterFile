using DailyOneRosterFile.Api.Models;

namespace DailyOneRosterFile.Api.Tests.Models;

public class FileVariantOptionsTests
{
    [Fact]
    public void DefaultSmallSchoolCount_Is3()
    {
        // Arrange
        var options = new FileVariantOptions();

        // Act & Assert
        Assert.Equal(3, options.SmallSchoolCount);
    }

    [Fact]
    public void DefaultLargeSchoolCount_Is22()
    {
        // Arrange
        var options = new FileVariantOptions();

        // Act & Assert
        Assert.Equal(22, options.LargeSchoolCount);
    }

    [Fact]
    public void SectionName_IsFileVariant()
    {
        // Act & Assert
        Assert.Equal("FileVariant", FileVariantOptions.SectionName);
    }
}
