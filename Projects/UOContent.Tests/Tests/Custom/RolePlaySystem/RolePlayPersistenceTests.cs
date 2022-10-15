using System;
using Server;
using Server.Accounting;
using Server.Custom.RolePlaySystem;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Tests.Custom.RolePlaySystem;

[Collection("Sequential Tests")]
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
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "aaaaaa";

        // When
        Assert.True(RolePlayPersistence.SetRolePlayRate(from1, target, 1));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from2, target, 2));
        Assert.True(RolePlayPersistence.SetRolePlayRate(from3, target, 3));

        // Then
        Assert.Equal(2, RolePlayPersistence.GetPlayerRolePlayRate(target));
    }

    [Fact]
    public void TestGetPlayerRolePlayRateInvalidRate()
    {
        // Given
        var target = new PlayerMobile();
        IAccount account = new Account((Serial)Random.Shared.NextInt64());
        target.Account = account;
        target.Name = "aaaaaa";

        // When
        var from1 = new PlayerMobile();

        // Then
        Assert.False(RolePlayPersistence.SetRolePlayRate(from1, target, -1));
    }

    [Fact]
    public void TestGetPlayerRolePlayRateEmpty()
    {
        // Given
        var target = new PlayerMobile();

        // When
        IAccount account = new Account((Serial)0x3030);
        target.Account = account;
        target.Name = "aaaaaa";

        // Then
        Assert.Null(RolePlayPersistence.GetPlayerRolePlayRate(target));
    }
}
