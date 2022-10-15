using System;
using Server;
using Server.Accounting;
using Server.Custom.RolePlaySystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.RolePlaySystem;

[Collection("Sequential Tests")]
public class RolePlayPersistenceGroupTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestGetPlayerRolePlayRate()
    {
        // Given
        var from1 = new PlayerMobile();
        var from2 = new PlayerMobile();
        var from3 = new PlayerMobile();

        var target2 = new PlayerMobile();
        IAccount account2 = new Account((Serial)Random.Shared.NextInt64());
        target2.Account = account2;
        target2.Name = "target2";

        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "target";

        // When
        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target, 1));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from2, target, 2));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from3, target, 3));

        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target2, 4));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from2, target2, 4));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from3, target2, 5));

        // Then
        Assert.Equal(2, RolePlayPersistence.GetPlayerRolePlayRate(target));
        Assert.Equal(4, RolePlayPersistence.GetPlayerRolePlayRate(target2));
    }
}
