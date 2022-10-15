using System;
using Server;
using Server.Accounting;
using Server.Custom.Classes;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.Classes;

[Collection("Sequential Tests")]
public class ClassPersistenceTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestGetClassPersistence()
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

        ClassPersistence.SetClass(target, Server.Custom.Classes.Classes.Agent, false);
        ClassPersistence.SetClass(target2, Server.Custom.Classes.Classes.Burglar, false);
        ClassPersistence.SetClass(target3, Server.Custom.Classes.Classes.Wanderer, false);

        Assert.Equal(Server.Custom.Classes.Classes.Burglar, ClassPersistence.GetPlayerClass(target2));
        Assert.Equal(Server.Custom.Classes.Classes.Agent, ClassPersistence.GetPlayerClass(target));
        Assert.Equal(Server.Custom.Classes.Classes.Wanderer, ClassPersistence.GetPlayerClass(target3));
    }

    [Fact]
    public void TestGetClassPersistencOverride()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        ClassPersistence.SetClass(target, Server.Custom.Classes.Classes.Agent, false);
        ClassPersistence.SetClass(target, Server.Custom.Classes.Classes.Warrior, true);

        Assert.Equal(Server.Custom.Classes.Classes.Warrior, ClassPersistence.GetPlayerClass(target));
    }

    [Fact]
    public void TestGetClassPersistencFalse()
    {
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        ClassPersistence.SetClass(target, Server.Custom.Classes.Classes.Agent, false);

        Assert.False(ClassPersistence.SetClass(target, Server.Custom.Classes.Classes.Warrior, false));
        Assert.Equal(Server.Custom.Classes.Classes.Agent, ClassPersistence.GetPlayerClass(target));
    }
}
