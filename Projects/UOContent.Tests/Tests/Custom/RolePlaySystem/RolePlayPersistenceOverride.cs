using System;
using Server;
using Server.Accounting;
using Server.Custom.RolePlaySystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.RolePlaySystem;

[Collection("Sequential Tests")]
public class RolePlayPersistenceOverride : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestGetPlayerRolePlayRateOverride()
    {
        // Given
        var from1 = new PlayerMobile();
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "aaaaaa";

        // When
        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target, 1));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target, 4));

        // Then
        Assert.Equal(4, RolePlayPersistence.GetPlayerRolePlayRate(target));
    }
}
