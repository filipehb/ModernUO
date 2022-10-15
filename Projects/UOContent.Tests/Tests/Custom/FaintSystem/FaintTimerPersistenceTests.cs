using System;
using Server;
using Server.Accounting;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.FaintSystem;

[Collection("Sequential Tests")]
public class FaintTimerPersistenceTests : IClassFixture<ServerFixture>
{

    [Fact]
    public void TestGetFaintTimerPersistence()
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

        FaintTimerPersistence.SetFaintRunning(target, false, false);
        FaintTimerPersistence.SetFaintRunning(target2, true, false);
        FaintTimerPersistence.SetFaintRunning(target3, false, false);

        Assert.Equal(true, FaintTimerPersistence.GetPlayerFaintRunning(target2));
        Assert.Equal(false, FaintTimerPersistence.GetPlayerFaintRunning(target));
        Assert.Equal(false, FaintTimerPersistence.GetPlayerFaintRunning(target3));
    }

    [Fact]
    public void TestGetRacePersistencOverride()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintTimerPersistence.SetFaintRunning(target, true, false);
        FaintTimerPersistence.SetFaintRunning(target, false, true);

        Assert.Equal(false, FaintTimerPersistence.GetPlayerFaintRunning(target));
    }

    [Fact]
    public void TestGetPlayerFaintRunningFalse()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintTimerPersistence.SetFaintRunning(target, true, false);

        Assert.False(FaintTimerPersistence.SetFaintRunning(target, false, false));
        Assert.Equal(true, FaintTimerPersistence.GetPlayerFaintRunning(target));
    }
}
