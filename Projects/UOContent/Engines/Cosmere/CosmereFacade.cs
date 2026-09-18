using Server.Mobiles;

namespace Server.Engines.Cosmere;

public static class CosmereFacade
{
    private static readonly SkillName[] _skillMap =
    [
        SkillName.Stealth,       // Agility
        SkillName.Wrestling,     // Athletics
        SkillName.Tinkering,     // Crafting
        SkillName.Begging,       // Deception
        SkillName.EvalInt,       // Deduction
        SkillName.Focus,         // Discipline
        SkillName.Macing,        // HeavyWeaponry
        SkillName.SpiritSpeak,   // Insight
        SkillName.Provocation,   // Intimidation
        SkillName.Peacemaking,   // Leadership
        SkillName.Fencing,       // LightWeaponry
        SkillName.ItemID,        // Lore
        SkillName.Healing,       // Medicine
        SkillName.DetectHidden,  // Perception
        SkillName.Musicianship,  // Persuasion
        SkillName.Hiding,        // Stealth
        SkillName.Tracking,      // Survival
        SkillName.Lockpicking,   // Thievery
        SkillName.Meditation     // Allomancy
    ];

    public static int ToUoStat(int cosmereAttr) =>
        System.Math.Clamp(10 + cosmereAttr * 18, 10, 100);

    public static void Apply(PlayerMobile pm)
    {
        var sheet = pm.CosmereSheet;
        if (sheet == null)
        {
            return;
        }

        sheet.ClampPools();

        pm.RawStr = ToUoStat(sheet.Strength);
        pm.RawDex = ToUoStat(sheet.Speed);
        pm.RawInt = ToUoStat(sheet.Intellect);

        // Lock all skills, then push Cosmere facade values.
        for (var i = 0; i < pm.Skills.Length; i++)
        {
            var skill = pm.Skills[i];
            skill.BaseFixedPoint = 0;
            skill.CapFixedPoint = 1000;
            skill.SetLockNoRelay(SkillLock.Locked);
        }

        for (var i = 0; i < CosmereSkills.Count; i++)
        {
            var uoSkill = pm.Skills[_skillMap[i]];
            var rank = sheet.GetRank((CosmereSkill)i);
            uoSkill.Base = System.Math.Clamp(rank * 20.0, 0.0, 100.0);
            uoSkill.SetLockNoRelay(SkillLock.Locked);
        }

        // HitsMax/StamMax/ManaMax come from PlayerMobile Cosmere overrides.
        pm.Hits = System.Math.Clamp(sheet.Health, 0, pm.HitsMax);
        pm.Stam = System.Math.Clamp(sheet.Focus, 0, pm.StamMax);
        pm.Mana = System.Math.Clamp(sheet.Investiture, 0, pm.ManaMax);
    }
}
