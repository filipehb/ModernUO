using System;
using System.IO;
using Server;
using Server.Engines.Cosmere;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

public class CosmereSheetTests
{
    [Fact]
    public void CreateMundaneStarter_MatchesPhase1Defaults()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();

        Assert.Equal(1, sheet.Level);
        Assert.Equal(1, sheet.Strength);
        Assert.Equal(1, sheet.Speed);
        Assert.Equal(1, sheet.Intellect);
        Assert.Equal(1, sheet.Willpower);
        Assert.Equal(1, sheet.Awareness);
        Assert.Equal(1, sheet.Presence);

        Assert.Equal(11, sheet.MaxHealth);
        Assert.Equal(0, sheet.LevelHealthBonus);
        Assert.Equal(3, sheet.MaxFocus);
        Assert.Equal(0, sheet.MaxInvestiture);
        Assert.Equal(11, sheet.Health);
        Assert.Equal(3, sheet.Focus);
        Assert.Equal(0, sheet.Investiture);

        Assert.Equal(12, sheet.PhysicalDefense);
        Assert.Equal(12, sheet.CognitiveDefense);
        Assert.Equal(12, sheet.SpiritualDefense);
        Assert.Equal(2, sheet.MaxSkillRank);
        Assert.Empty(sheet.Expertises);

        for (var i = 0; i < CosmereSkills.Count; i++)
        {
            Assert.Equal(0, sheet.GetRank((CosmereSkill)i));
        }
    }

    [Fact]
    public void GetModifier_IsAttributePlusRank()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Strength = 3;
        sheet.SetRank(CosmereSkill.Athletics, 2);

        Assert.Equal(5, sheet.GetModifier(CosmereSkill.Athletics));
    }

    [Fact]
    public void SetRank_ClampsToTierCap()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.SetRank(CosmereSkill.Lore, 99);

        Assert.Equal(2, sheet.GetRank(CosmereSkill.Lore));
    }

    [Fact]
    public void MaxSkillRank_FollowsLevelTiers()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();

        sheet.Level = 5;
        Assert.Equal(2, sheet.MaxSkillRank);
        sheet.Level = 6;
        Assert.Equal(3, sheet.MaxSkillRank);
        sheet.Level = 11;
        Assert.Equal(4, sheet.MaxSkillRank);
        sheet.Level = 16;
        Assert.Equal(5, sheet.MaxSkillRank);
    }

    [Fact]
    public void ClampPools_RespectsMax()
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Health = 99;
        sheet.Focus = -1;
        sheet.ClampPools();

        Assert.Equal(sheet.MaxHealth, sheet.Health);
        Assert.Equal(0, sheet.Focus);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 6)]
    [InlineData(2, 6)]
    [InlineData(3, 8)]
    [InlineData(5, 10)]
    [InlineData(7, 12)]
    [InlineData(9, 20)]
    public void GetRecoveryDieSides_MatchesWillpowerBands(int willpower, int sides)
    {
        var sheet = CosmereSheet.CreateMundaneStarter();
        sheet.Willpower = willpower;
        Assert.Equal(sides, sheet.GetRecoveryDieSides());
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var original = CosmereSheet.CreateMundaneStarter();
        original.Level = 4;
        original.Strength = 2;
        original.Speed = 3;
        original.Intellect = 2;
        original.Willpower = 1;
        original.Awareness = 4;
        original.Presence = 2;
        original.SetRank(CosmereSkill.Athletics, 2);
        original.SetRank(CosmereSkill.Stealth, 1);
        original.Health = 8;
        original.Focus = 2;
        original.Expertises.Add("Streetwise");
        original.Dead = true;
        original.AddExhausted(2, "test");
        original.EnsureCondition(CosmereConditionKind.Slowed, source: "test");
        original.Injuries.Add(
            new CosmereInjuryRecord
            {
                Id = 7,
                Severity = CosmereInjurySeverity.Vicious,
                EffectKind = CosmereConditionKind.Exhausted,
                EffectMagnitude = 1,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(3),
                Cleared = false
            }
        );
        original.NextInjuryId = 8;
        original.IsMetalborn = true;
        original.AllomanticMetal = CosmereAllomanticMetal.Steel;
        original.PowerTier = CosmerePowerTier.Nascent;
        original.LevelHealthBonus = 5;
        original.PendingAttrPoints = 1;
        original.PendingSkillRanks = 2;
        original.PendingTalentPicks = 1;
        original.Talents.Add("steelrunner");
        original.Goals.Add(
            new CosmereGoalRecord
            {
                Id = CosmereGoals.TrainYourPowerId,
                Title = CosmereGoals.TrainYourPowerTitle,
                Status = CosmereGoalStatus.Active,
                RewardKind = CosmereGoalRewardKind.UnlockFullAllomancy,
                RewardPayload = ""
            }
        );
        original.ProfessionId = CosmereProfessionId.Artisan;
        original.ProfessionRank = 3;
        original.ProfessionXp = 120;

        var writer = new BufferWriter(true);
        original.Serialize(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
        writer.Close();

        var copy = new CosmereSheet();
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(original.Level, copy.Level);
        Assert.Equal(original.Strength, copy.Strength);
        Assert.Equal(original.Speed, copy.Speed);
        Assert.Equal(original.Intellect, copy.Intellect);
        Assert.Equal(original.Willpower, copy.Willpower);
        Assert.Equal(original.Awareness, copy.Awareness);
        Assert.Equal(original.Presence, copy.Presence);
        Assert.Equal(original.Health, copy.Health);
        Assert.Equal(original.Focus, copy.Focus);
        Assert.Equal(original.Investiture, copy.Investiture);
        Assert.Equal(original.GetRank(CosmereSkill.Athletics), copy.GetRank(CosmereSkill.Athletics));
        Assert.Equal(original.GetRank(CosmereSkill.Stealth), copy.GetRank(CosmereSkill.Stealth));
        Assert.Equal(original.Expertises, copy.Expertises);
        Assert.True(copy.Dead);
        Assert.Equal(2, copy.GetExhaustedMagnitude());
        Assert.True(copy.HasCondition(CosmereConditionKind.Slowed));
        Assert.Equal(1, copy.ActiveInjuryCount);
        Assert.Equal(CosmereInjurySeverity.Vicious, copy.Injuries[0].Severity);
        Assert.Equal(8, copy.NextInjuryId);
        Assert.True(copy.IsMetalborn);
        Assert.Equal(CosmereAllomanticMetal.Steel, copy.AllomanticMetal);
        Assert.Equal(CosmerePowerTier.Nascent, copy.PowerTier);
        Assert.Equal(5, copy.LevelHealthBonus);
        Assert.Equal(1, copy.PendingAttrPoints);
        Assert.Equal(2, copy.PendingSkillRanks);
        Assert.Equal(1, copy.PendingTalentPicks);
        Assert.Equal(["steelrunner"], copy.Talents);
        Assert.Single(copy.Goals);
        Assert.Equal(CosmereGoals.TrainYourPowerId, copy.Goals[0].Id);
        Assert.Equal(CosmereGoalStatus.Active, copy.Goals[0].Status);
        Assert.Equal(CosmereProfessionId.Artisan, copy.ProfessionId);
        Assert.Equal(3, copy.ProfessionRank);
        Assert.Equal(120, copy.ProfessionXp);
    }
}
