using Server.Engines.Cosmere;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

public class CosmereTimeTests
{
    [Theory]
    [InlineData(6, CosmereDayPeriod.Morning)]
    [InlineData(11, CosmereDayPeriod.Morning)]
    [InlineData(12, CosmereDayPeriod.Afternoon)]
    [InlineData(16, CosmereDayPeriod.Afternoon)]
    [InlineData(17, CosmereDayPeriod.Evening)]
    [InlineData(21, CosmereDayPeriod.Evening)]
    [InlineData(22, CosmereDayPeriod.Night)]
    [InlineData(0, CosmereDayPeriod.Night)]
    [InlineData(5, CosmereDayPeriod.Night)]
    public void GetPeriod_MapsHours(int hour, CosmereDayPeriod expected) =>
        Assert.Equal(expected, CosmereTime.GetPeriod(hour));
}
