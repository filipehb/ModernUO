using System;
using System.Collections.Generic;
using Server.Custom.AgeSystem;
using Server.Custom.Race;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.AgeSystem;

public class AgeUtilsTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestAgeInDays()
    {
        var time = DateTime.UtcNow;
        double ageInDays = AgeUtils.AgeInDays(time);
        Assert.NotEqual(0.0, ageInDays);
    }

    [Theory]
    [InlineData(31, -1)]
    [InlineData(0, 0)]
    [InlineData(-31, 1)]
    [InlineData(-62, 2)]
    public void TestAgeInMonths(int days, int expected)
    {
        var time = DateTime.UtcNow.AddDays(days);
        double ageInDays = AgeUtils.AgeInMonths(time);
        Assert.Equal(expected, ageInDays);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-31, 1)]
    [InlineData(-62, 2)]
    public void TestAgeInMonthsWithoutTrucate(int days, int expected)
    {
        var time = DateTime.UtcNow.AddDays(days);
        double ageInDays = AgeUtils.AgeInMonthsWithoutTrucate(time);
        Assert.True(ageInDays > expected);
    }

    [Fact]
    public void TestGenerateDeath()
    {
        var resultList = new List<string>
        {
            "Uma flecha atingiu seu peito e você morreu", "Você pisou onde não devia e morreu",
            "Algo caiu dos céus e foi bem em você", "Você faleceu de causas naturais",
            "você sofreu uma combustão espontânea", "Lembra daquela unha encravada? Você morreu"
        };
        Assert.Contains(AgeUtils.GenerateDeath(), resultList);
    }

    [Theory]
    [InlineData(Races.Dwarve, 120, true)]
    [InlineData(Races.Elve, 12000, false)]
    [InlineData(Races.Dwarve, 16, false)]
    [InlineData(Races.Human, 60, true)]
    [InlineData(Races.Human, 120, true)]
    public void TestIsLifeLimit(Races race, int age, bool expected)
    {
        var player = new PlayerMobile();
        RacePersistence.SetRace(player, race, true);
        AgePersistence.SetAge(player, age, true);
        player.Created = DateTime.Now;

        bool isLifeLimit = AgeUtils.IsLifeLimit(player);
        Assert.Equal(expected, isLifeLimit);
    }
}
