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
        // Given
        var from1 = new PlayerMobile();
        var from2 = new PlayerMobile();
        var from3 = new PlayerMobile();
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)0x3000);
        target.Account = account;
        target.Name = "aaaaaa";

        // When
        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target, 1));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from2, target, 2));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from3, target, 3));

        // Then
        Assert.Equal(2, RolePlayPersistence.GetPlayerRolePlayRate(target));
    }
}
