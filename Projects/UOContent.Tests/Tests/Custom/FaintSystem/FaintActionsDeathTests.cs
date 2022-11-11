using System;
using Server;
using Server.Accounting;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.FaintSystem;

[Collection("Sequential Tests")]
public class FaintActionsDeathTests : IClassFixture<ServerFixture>
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
}
