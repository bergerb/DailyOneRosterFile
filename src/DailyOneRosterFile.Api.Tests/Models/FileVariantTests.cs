using DailyOneRosterFile.Api.Models;

namespace DailyOneRosterFile.Api.Tests.Models;

public class FileVariantTests
{
    [Fact]
    public void Small_EqualsLowercaseSmall()
    {
        Assert.Equal("small", FileVariant.Small);
    }

    [Fact]
    public void Large_EqualsLowercaseLarge()
    {
        Assert.Equal("large", FileVariant.Large);
    }
}
