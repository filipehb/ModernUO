using System;
using Server;
using Server.Accounting;
using Server.Custom.AgeSystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.AgeSystem;

[Collection("Sequential Tests")]
public class AgePersistenceTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestIncreaseAge()
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

        AgePersistence.SetAge(target, 16, false);
        AgePersistence.SetAge(target2, 22, false);
        AgePersistence.SetAge(target3, 1000, false);

        AgePersistence.IncreaseAges();

        Assert.Equal(23, AgePersistence.GetPlayerAge(target2));
        Assert.Equal(17, AgePersistence.GetPlayerAge(target));
        Assert.Equal(1001, AgePersistence.GetPlayerAge(target3));
    }

    [Fact]
    public void TestGetAge()
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

        AgePersistence.SetAge(target, 16, false);
        AgePersistence.SetAge(target2, 22, false);
        AgePersistence.SetAge(target3, 1000, false);

        Assert.Equal(22, AgePersistence.GetPlayerAge(target2));
        Assert.Equal(16, AgePersistence.GetPlayerAge(target));
        Assert.Equal(1000, AgePersistence.GetPlayerAge(target3));
    }

    [Fact]
    public void TestGetRacePersistencOverride()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        AgePersistence.SetAge(target, 17, false);
        AgePersistence.SetAge(target, 177, true);

        Assert.Equal(177,AgePersistence.GetPlayerAge(target));
    }

    [Fact]
    public void TestGetPlayerFaintRunningFalse()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        AgePersistence.SetAge(target, 16, false);

        Assert.False(AgePersistence.SetAge(target, 2000, false));
        Assert.Equal(16, AgePersistence.GetPlayerAge(target));
    }
}
