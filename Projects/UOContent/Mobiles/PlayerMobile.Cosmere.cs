using System;
using Sheet = Server.Engines.Cosmere.CosmereSheet;
using Server.Engines.Cosmere;

namespace Server.Mobiles;

public partial class PlayerMobile
{
    private Sheet _cosmereSheet;
    private CosmereCombatState _cosmereCombat;
    private CosmereAllomancyState _cosmereAllomancy;
    private CosmereConversationState _cosmereConversation;

    /// <summary>Session-only Cosmere debug SysMessages. Not serialized.</summary>
    public bool CosmereDebug { get; set; }

    /// <summary>Session-only last announced day period. Not serialized.</summary>
    public CosmereDayPeriod? CosmereLastDayPeriod { get; set; }

    /// <summary>Session-only throttle for sustenance reminders. Not serialized.</summary>
    public DateTime CosmereSustenanceLastReminder { get; set; }

    /// <summary>Reentrant guard so Cosmere→Hits sync does not re-enter Damage conversion.</summary>
    internal int _cosmereDamageDepth;

    [CommandProperty(AccessLevel.GameMaster)]
    public Sheet CosmereSheet
    {
        get => _cosmereSheet ??= Sheet.CreateMundaneStarter();
        set => _cosmereSheet = value ?? Sheet.CreateMundaneStarter();
    }

    public bool HasCosmereSheet => _cosmereSheet != null;

    public CosmereCombatState CosmereCombat
    {
        get => _cosmereCombat;
        set => _cosmereCombat = value;
    }

    public CosmereAllomancyState CosmereAllomancy
    {
        get => _cosmereAllomancy;
        set => _cosmereAllomancy = value;
    }

    public CosmereConversationState CosmereConversation
    {
        get => _cosmereConversation;
        set => _cosmereConversation = value;
    }

    public void EnsureCosmereSheet()
    {
        _cosmereSheet ??= Sheet.CreateMundaneStarter();
    }

    public void ApplyCosmereFacade()
    {
        EnsureCosmereSheet();
        CosmereFacade.Apply(this);
    }

    public override void OnHungerChanged(int oldValue)
    {
        base.OnHungerChanged(oldValue);
        if (HasCosmereSheet)
        {
            CosmereSustenance.Sync(this);
        }
    }

    public override void OnThirstChanged(int oldValue)
    {
        base.OnThirstChanged(oldValue);
        if (HasCosmereSheet)
        {
            CosmereSustenance.Sync(this);
        }
    }
}
