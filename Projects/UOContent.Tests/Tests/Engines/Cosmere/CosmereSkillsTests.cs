using Server.Engines.Cosmere;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

public class CosmereSkillsTests
{
    [Fact]
    public void Count_IsNineteen() => Assert.Equal(19, CosmereSkills.Count);

    [Theory]
    [InlineData(CosmereSkill.Athletics, CosmereAttribute.Strength)]
    [InlineData(CosmereSkill.Agility, CosmereAttribute.Speed)]
    [InlineData(CosmereSkill.Stealth, CosmereAttribute.Speed)]
    [InlineData(CosmereSkill.Discipline, CosmereAttribute.Willpower)]
    [InlineData(CosmereSkill.Perception, CosmereAttribute.Awareness)]
    [InlineData(CosmereSkill.Persuasion, CosmereAttribute.Presence)]
    [InlineData(CosmereSkill.Lore, CosmereAttribute.Intellect)]
    [InlineData(CosmereSkill.Allomancy, CosmereAttribute.Willpower)]
    public void GetAttribute_MatchesSpec(CosmereSkill skill, CosmereAttribute expected) =>
        Assert.Equal(expected, CosmereSkills.GetAttribute(skill));

    [Theory]
    [InlineData("Athletics", CosmereSkill.Athletics)]
    [InlineData("athletics", CosmereSkill.Athletics)]
    [InlineData("Heavy Weaponry", CosmereSkill.HeavyWeaponry)]
    [InlineData("HeavyWeaponry", CosmereSkill.HeavyWeaponry)]
    [InlineData("heavy_weaponry", CosmereSkill.HeavyWeaponry)]
    [InlineData("LightWeaponry", CosmereSkill.LightWeaponry)]
    [InlineData("Allomancy", CosmereSkill.Allomancy)]
    public void TryParse_AcceptsCommonForms(string text, CosmereSkill expected)
    {
        Assert.True(CosmereSkills.TryParse(text, out var skill));
        Assert.Equal(expected, skill);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NotASkill")]
    public void TryParse_RejectsUnknown(string text) =>
        Assert.False(CosmereSkills.TryParse(text, out _));

    [Fact]
    public void GetDisplayName_UsesSpacedLabels()
    {
        Assert.Equal("Heavy Weaponry", CosmereSkills.GetDisplayName(CosmereSkill.HeavyWeaponry));
        Assert.Equal("Athletics", CosmereSkills.GetDisplayName(CosmereSkill.Athletics));
    }
}
