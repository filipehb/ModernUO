using System;
using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereKnownNamesTests
{
    public CosmereKnownNamesTests()
    {
        CosmereConfig.SetKnownNamesEnabledForTests(true);
        CosmereConfig.SetUnknownNameDisplayForTests("A stranger");
    }

    [Fact]
    public void Resolve_Unknown_IsConfiguredStranger()
    {
        var viewer = CreatePlayer("Viewer");
        var subject = CreatePlayer("Subject");

        try
        {
            Assert.Equal("A stranger", CosmereKnownNames.Resolve(viewer, subject));
            Assert.Equal("A stranger", subject.GetNameFor(viewer));
        }
        finally
        {
            viewer.Delete();
            subject.Delete();
        }
    }

    [Fact]
    public void Resolve_KnownLabel_IsReturned()
    {
        var viewer = CreatePlayer("Viewer");
        var subject = CreatePlayer("Subject");

        try
        {
            CosmereKnownNames.Set(viewer, subject.Serial, "Vin", "mistborn?");
            Assert.Equal("Vin", CosmereKnownNames.Resolve(viewer, subject));
            Assert.True(CosmereKnownNames.TryGetEntry(viewer, subject.Serial, out var entry));
            Assert.Equal("mistborn?", entry.Notes);
        }
        finally
        {
            viewer.Delete();
            subject.Delete();
        }
    }

    [Fact]
    public void Resolve_StaffSeesRawName()
    {
        var staff = CreatePlayer("Staff");
        staff.AccessLevel = AccessLevel.Counselor;
        var subject = CreatePlayer("RealName");

        try
        {
            CosmereKnownNames.Set(staff, subject.Serial, "Alias", "");
            Assert.Equal("RealName", CosmereKnownNames.Resolve(staff, subject));
        }
        finally
        {
            staff.Delete();
            subject.Delete();
        }
    }

    [Fact]
    public void Resolve_SelfSeesOwnName()
    {
        var pm = CreatePlayer("Myself");
        try
        {
            Assert.Equal("Myself", CosmereKnownNames.Resolve(pm, pm));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Resolve_NameMod_BreaksKnownBond()
    {
        var viewer = CreatePlayer("Viewer");
        var subject = CreatePlayer("Subject");

        try
        {
            CosmereKnownNames.Set(viewer, subject.Serial, "Known", "");
            subject.NameMod = "DisguiseBob";
            Assert.Equal("DisguiseBob", CosmereKnownNames.Resolve(viewer, subject));

            subject.NameMod = null;
            Assert.Equal("Known", CosmereKnownNames.Resolve(viewer, subject));
        }
        finally
        {
            viewer.Delete();
            subject.Delete();
        }
    }

    [Fact]
    public void TruncateLabel_CapsAt25()
    {
        var longName = new string('x', 40);
        var truncated = CosmereKnownNames.TruncateLabel(longName);
        Assert.Equal(25, truncated.Length);
    }

    [Fact]
    public void Serialize_RoundTrip_KnownMap()
    {
        var viewer = CreatePlayer("Viewer");
        var subject = CreatePlayer("Subject");

        try
        {
            CosmereKnownNames.Set(viewer, subject.Serial, "Label", "notes");

            var writer = new BufferWriter(true);
            CosmereKnownNames.Serialize(viewer, writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
            writer.Close();

            var other = CreatePlayer("Other");
            try
            {
                CosmereKnownNames.Deserialize(other, (IGenericReader)new BufferReader(buffer));
                Assert.True(CosmereKnownNames.TryGetEntry(other, subject.Serial, out var entry));
                Assert.Equal("Label", entry.Label);
                Assert.Equal("notes", entry.Notes);
            }
            finally
            {
                other.Delete();
            }
        }
        finally
        {
            viewer.Delete();
            subject.Delete();
        }
    }

    [Fact]
    public void Npc_UsesNormalName()
    {
        var viewer = CreatePlayer("Viewer");
        var wolf = new DireWolf { Name = "a dire wolf" };

        try
        {
            Assert.Equal("a dire wolf", CosmereKnownNames.Resolve(viewer, wolf));
        }
        finally
        {
            viewer.Delete();
            wolf.Delete();
        }
    }

    private static PlayerMobile CreatePlayer(string name)
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        pm.Name = name;
        return pm;
    }
}
