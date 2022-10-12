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
    [Fact]
    public void TestGetPlayerRolePlayRate()
    {
        var from1 = new PlayerMobile();
        var from2 = new PlayerMobile();
        var from3 = new PlayerMobile();
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)0x3000);
        target.Account = account;
        target.Name = "aaaaaa";
        //Todo Investigar pq o modelo dic innerDic tá sobrescrevendo o outro resultado. Nesse cenárip fica sempre o 3 como rate e como media, pq ele é o final e a media
        RolePlayPersistence.SetRolePlayRate(from1, target, 1, true);
        RolePlayPersistence.SetRolePlayRate(from2, target, 2, true);
        RolePlayPersistence.SetRolePlayRate(from3, target, 3, true);
        Assert.Equal(2, RolePlayPersistence.GetPlayerRolePlayRate(target));
    }
}
