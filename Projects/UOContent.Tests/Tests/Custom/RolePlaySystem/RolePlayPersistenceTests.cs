using System;
using Server;
using Server.Accounting;
using Server.Custom.AgeSystem;
using Server.Custom.RolePlaySystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.RolePlaySystem;

public class RolePlayPersistenceTests : IClassFixture<ServerFixture>
{
    [Theory]
    [InlineData(1, 2, 2, 1)]
    [InlineData(1, 2, 0, 1)]
    [InlineData(3, 2, 1, 1)]
    [InlineData(5, 5, 5, 5)]
    [InlineData(0, 0, 0, 0)]
    public void TestGetPlayerRolePlayRate(int rate1, int rate2, int rate3, int mean)
    {
        var from1 = new PlayerMobile();
        var from2 = new PlayerMobile();
        var from3 = new PlayerMobile();
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)0x3000);
        target.Account = account;
        target.Name = "adsdas";
        RolePlayPersistence.SetRolePlayRate(from1, target, rate1, false);
        RolePlayPersistence.SetRolePlayRate(from2, target, rate2, false);
        RolePlayPersistence.SetRolePlayRate(from3, target, rate3, false);
        Assert.Equal(mean, RolePlayPersistence.GetPlayerRolePlayRate(target));
    }
}
