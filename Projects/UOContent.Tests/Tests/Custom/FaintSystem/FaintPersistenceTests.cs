using System;
using Server;
using Server.Accounting;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.FaintSystem;

[Collection("Sequential Tests")]
public class FaintPersistenceTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestGetFaintPersistence()
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

        FaintPersistence.SetFaint(target, 3, false);
        FaintPersistence.SetFaint(target2, 2, false);
        FaintPersistence.SetFaint(target3, 1, false);

        Assert.Equal(2, FaintPersistence.GetPlayerFaint(target2));
        Assert.Equal(3, FaintPersistence.GetPlayerFaint(target));
        Assert.Equal(1, FaintPersistence.GetPlayerFaint(target3));
    }

    [Fact]
    public void TestGetFaintPersistenceOverride()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, 4, false);
        FaintPersistence.SetFaint(target, 1, true);

        Assert.Equal(1, FaintPersistence.GetPlayerFaint(target));
    }

    [Fact]
    public void TestGetFaintPersistenceFalse()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        FaintPersistence.SetFaint(target, 4, false);

        Assert.False(FaintPersistence.SetFaint(target, 1, false));
        Assert.Equal(4, FaintPersistence.GetPlayerFaint(target));
    }
}
