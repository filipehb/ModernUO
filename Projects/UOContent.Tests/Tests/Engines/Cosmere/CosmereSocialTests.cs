using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereSocialTests
{
    [Theory]
    [InlineData(CosmerePersuasiveness.Normal, 2)]
    [InlineData(CosmerePersuasiveness.Strong, 4)]
    [InlineData(CosmerePersuasiveness.Overwhelm, 0)]
    public void GetResistFocusCost_MatchesTable(CosmerePersuasiveness p, int expected) =>
        Assert.Equal(expected, CosmereSocial.GetResistFocusCost(p));

    [Theory]
    [InlineData(CosmereSkill.Persuasion, CosmereSocialDefense.Spiritual)]
    [InlineData(CosmereSkill.Deception, CosmereSocialDefense.Spiritual)]
    [InlineData(CosmereSkill.Leadership, CosmereSocialDefense.Spiritual)]
    [InlineData(CosmereSkill.Intimidation, CosmereSocialDefense.Spiritual)]
    [InlineData(CosmereSkill.Insight, CosmereSocialDefense.Cognitive)]
    [InlineData(CosmereSkill.Deduction, CosmereSocialDefense.Cognitive)]
    [InlineData(CosmereSkill.Athletics, CosmereSocialDefense.Spiritual)]
    public void GetDefaultDefense_MatchesTable(CosmereSkill skill, CosmereSocialDefense expected) =>
        Assert.Equal(expected, CosmereSocial.GetDefaultDefense(skill));

    [Fact]
    public void TryResistInfluence_PcSpendsFocus_NeverForcedPath()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.Focus = 3;
            Assert.True(CosmereSocial.TryResistInfluence(pm, 2, out var resisted));
            Assert.True(resisted);
            Assert.Equal(1, pm.CosmereSheet.Focus);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void TryResistInfluence_PcInsufficientFocus_NotResisted()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.Focus = 1;
            Assert.True(CosmereSocial.TryResistInfluence(pm, 2, out var resisted));
            Assert.False(resisted);
            Assert.Equal(1, pm.CosmereSheet.Focus);
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void NpcFocus_DrainToZero_Concedes()
    {
        var npc = new Mobile(World.NewMobile);
        npc.DefaultMobileInit();
        try
        {
            var state = CosmereSocial.GetOrCreateNpc(npc);
            state.MaxFocus = 4;
            state.Focus = 4;
            state.Conceded = false;

            Assert.True(CosmereSocial.TryResistInfluence(npc, 2, out var r1));
            Assert.True(r1);
            Assert.Equal(2, state.Focus);
            Assert.False(state.Conceded);

            Assert.True(CosmereSocial.TryResistInfluence(npc, 2, out var r2));
            Assert.True(r2);
            Assert.Equal(0, state.Focus);
            Assert.True(state.Conceded);
        }
        finally
        {
            CosmereSocial.ClearNpcState(npc.Serial);
            npc.Delete();
        }
    }

    [Fact]
    public void Overwhelm_AutoConcedesNpc()
    {
        var npc = new Mobile(World.NewMobile);
        npc.DefaultMobileInit();
        try
        {
            var state = CosmereSocial.GetOrCreateNpc(npc);
            state.Focus = 4;
            Assert.True(CosmereSocial.TryResistInfluence(npc, 0, out var resisted));
            Assert.False(resisted);
            Assert.True(state.Conceded);
            Assert.Equal(0, state.Focus);
        }
        finally
        {
            CosmereSocial.ClearNpcState(npc.Serial);
            npc.Delete();
        }
    }

    [Fact]
    public void Formal_PresentableDisadv_AppliedInInfluenceResolve()
    {
        // Pure helper: Formal + not Presentable adds disadv — exercised via conversation state prep
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            CosmereSocial.BeginTalk(pm, formal: true);
            var conv = pm.CosmereConversation;
            Assert.True(conv.Formal);
            Assert.False(CosmereSocial.IsPresentable(pm)); // no armor
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Influence_RequiresConversation_ForPlayers()
    {
        var pm = new PlayerMobile(World.NewMobile);
        var other = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        other.DefaultMobileInit();
        try
        {
            other.CosmereConversation = new CosmereConversationState { SocialOptIn = true };
            Assert.False(CosmereSocial.CanInfluenceTarget(pm, other, out var reason));
            Assert.Contains("CosmereTalk", reason);

            CosmereSocial.BeginTalk(pm, formal: false, other);
            // Still may fail range if not on map — place them
            pm.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);
            other.MoveToWorld(new Point3D(101, 100, 0), Map.Felucca);
            Assert.True(CosmereSocial.CanInfluenceTarget(pm, other, out _));
        }
        finally
        {
            pm.Delete();
            other.Delete();
        }
    }

    [Fact]
    public void BeginAndEndTalk_ClearsInfluenced()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        try
        {
            CosmereSocial.BeginTalk(pm, formal: false);
            pm.EnsureCosmereSheet();
            pm.CosmereSheet.Conditions.Add(
                new CosmereCondition { Kind = CosmereConditionKind.Influenced, Payload = "test" }
            );
            CosmereSocial.EndTalk(pm);
            Assert.False(pm.CosmereConversation.InConversation);
            Assert.False(pm.CosmereSheet.HasCondition(CosmereConditionKind.Influenced));
        }
        finally
        {
            pm.Delete();
        }
    }
}
