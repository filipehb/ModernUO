using System;
using Server;
using Server.Accounting;
using Server.Custom.Race;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.Race;

[Collection("Sequential Tests")]
public class RacePersistenceTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestGetRacePersistence()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        var target2 = new PlayerMobile();
        IAccount account2 = new Account((Serial)Random.Shared.NextInt64());
        target2.Account = account2;
        target2.Name = "target2";

        var target3 = new PlayerMobile();
        IAccount account3 = new Account((Serial)Random.Shared.NextInt64());
        target3.Account = account3;
        target3.Name = "target3";

        RacePersistence.SetRace(target, Races.Human, false);
        RacePersistence.SetRace(target2, Races.Dwarve, false);
        RacePersistence.SetRace(target3, Races.Elve, false);

        Assert.Equal(Races.Dwarve, RacePersistence.GetPlayerRace(target2));
        Assert.Equal(Races.Human, RacePersistence.GetPlayerRace(target));
        Assert.Equal(Races.Elve, RacePersistence.GetPlayerRace(target3));
    }

    [Fact]
    public void TestGetRacePersistencOverride()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        RacePersistence.SetRace(target, Races.Human, false);
        RacePersistence.SetRace(target, Races.Dwarve, true);

        Assert.Equal(Races.Dwarve, RacePersistence.GetPlayerRace(target));
    }

    [Fact]
    public void TestGetRacePersistencFalse()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        RacePersistence.SetRace(target, Races.Human, false);

        Assert.False(RacePersistence.SetRace(target, Races.Dwarve, false));
        Assert.Equal(Races.Human, RacePersistence.GetPlayerRace(target));
    }
}
