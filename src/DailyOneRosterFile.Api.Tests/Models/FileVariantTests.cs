using DailyOneRosterFile.Api.Models;

namespace DailyOneRosterFile.Api.Tests.Models;

public class FileVariantTests
{
    [Fact]
    public void Small_EqualsLowercaseSmall()
    {
        // Act & Assert
        Assert.Equal("small", FileVariant.Small);
    }

    [Fact]
    public void Large_EqualsLowercaseLarge()
    {
        // Act & Assert
        Assert.Equal("large", FileVariant.Large);
    }
}
