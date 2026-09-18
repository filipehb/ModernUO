using Server;
using Server.Engines.Cosmere;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereStrikeTests
{
    [Fact]
    public void GetAttackSkill_Fists_IsAthletics()
    {
        Assert.Equal(CosmereSkill.Athletics, CosmereStrike.GetAttackSkill(null));
        Assert.Equal(CosmereSkill.Athletics, CosmereStrike.GetAttackSkill(new Fists()));
    }

    [Fact]
    public void GetDamageDieSides_UnarmedBands_AndClubIsHammer()
    {
        Assert.Equal(0, CosmereStrike.GetDamageDieSides(null, strength: 1));
        Assert.Equal(4, CosmereStrike.GetDamageDieSides(null, strength: 3));
        Assert.Equal(8, CosmereStrike.GetDamageDieSides(null, strength: 5));
        Assert.Equal(6, CosmereStrike.GetDamageDieSides(null, strength: 7)); // 2d6 → sides 6

        var club = new Club();
        try
        {
            Assert.Equal(10, CosmereStrike.GetDamageDieSides(club));
        }
        finally
        {
            club.Delete();
        }
    }

    [Fact]
    public void ComputeDamageOutcome_Hit_AddsModifier()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Strength = 2;
        sheet.SetRank(CosmereSkill.Athletics, 1);

        CosmereStrike.ComputeDamageOutcome(
            sheet,
            testSuccess: true,
            damageRoll: 3,
            CosmereSkill.Athletics,
            out var outcome,
            out var damage
        );

        Assert.Equal(CosmereStrikeOutcome.Hit, outcome);
        Assert.Equal(3 + 3, damage); // die + mod (Str2+rank1)
    }

    [Fact]
    public void ComputeDamageOutcome_MissWithFocus_GrazesWhenAutoGraze()
    {
        Assert.True(CosmereConfig.AutoGraze);

        var sheet = CosmereSheet.CreateMundaneStarter();
        var focusBefore = sheet.Focus;

        CosmereStrike.ComputeDamageOutcome(
            sheet,
            testSuccess: false,
            damageRoll: 4,
            CosmereSkill.Athletics,
            out var outcome,
            out var damage
        );

        Assert.Equal(CosmereStrikeOutcome.Graze, outcome);
        Assert.Equal(4, damage);
        Assert.Equal(focusBefore - 1, sheet.Focus);
    }

    [Fact]
    public void ComputeDamageOutcome_MissWithoutFocus_IsMiss()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Focus = 0;

        CosmereStrike.ComputeDamageOutcome(
            sheet,
            testSuccess: false,
            damageRoll: 4,
            CosmereSkill.Athletics,
            out var outcome,
            out var damage
        );

        Assert.Equal(CosmereStrikeOutcome.Miss, outcome);
        Assert.Equal(0, damage);
    }

    [Fact]
    public void GetDefenseDc_UsesNpcStubConstant()
    {
        Assert.Equal(12, CosmereConfig.NpcPhysicalDefense);
        Assert.True(CosmereConfig.NpcPhysicalDefense > 0);
    }
}
