using System;
using Server;
using Server.Accounting;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.FaintSystem;

[Collection("Sequential Tests")]
public class FaintActionsTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestPlayerDeath()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, 3, false);
        FaintAction.Initialize();

        EventSink.InvokePlayerDeath(target);

        Assert.Equal(2, FaintPersistence.GetPlayerFaint(target));
    }

    [Fact]
    public void TestPlayerDisconnect()
    {
        var target = new PlayerMobile();
        target.AccessLevel = AccessLevel.Player;
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, 3, false);
        FaintTimerPersistence.SetFaintRunning(target, true, false);

        FaintAction.Initialize();

        target.Kill();

        EventSink.InvokeDisconnected(target);

        Assert.False(FaintTimerPersistence.GetPlayerFaintRunning(target));
    }

    [Fact]
    public void TestPlayerDisconnectWithNegativeFaints()
    {
        var target = new PlayerMobile();
        target.AccessLevel = AccessLevel.Player;
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, -1, false);
        FaintTimerPersistence.SetFaintRunning(target, true, false);

        FaintAction.Initialize();

        target.Kill();

        EventSink.InvokeDisconnected(target);

        Assert.True(FaintTimerPersistence.GetPlayerFaintRunning(target));
    }

    [Fact]
    public void TestPlayerLogin()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, 3, false);
        FaintTimerPersistence.SetFaintRunning(target, false, false);
        FaintAction.Initialize();

        target.Kill();

        EventSink.InvokeLogin(target);

        Assert.True(FaintTimerPersistence.GetPlayerFaintRunning(target));
    }
}
