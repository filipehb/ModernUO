using System;
using System.Collections.Generic;
using Sheet = Server.Engines.Cosmere.CosmereSheet;
using Server.Engines.Cosmere;
using Server.Guilds;
using Server.Network;

namespace Server.Mobiles;

public partial class PlayerMobile
{
    private Sheet _cosmereSheet;
    private CosmereCombatState _cosmereCombat;
    private CosmereAllomancyState _cosmereAllomancy;
    private CosmereConversationState _cosmereConversation;
    internal Dictionary<Serial, CosmereKnownNameEntry> _knownNames;

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

    public override string GetNameFor(Mobile viewer) => CosmereKnownNames.Resolve(viewer, this);

    private Mobile _oplViewer;

    public override void SendPropertiesTo(NetState ns)
    {
        if (!CosmereKnownNames.IsEnabled || ns?.Mobile == null)
        {
            base.SendPropertiesTo(ns);
            return;
        }

        _oplViewer = ns.Mobile;
        try
        {
            var list = new ObjectPropertyList(this);
            GetProperties(list);
            list.Terminate();
            ns.Send(list.Buffer);
        }
        finally
        {
            _oplViewer = null;
        }
    }

    public override void AddNameProperties(IPropertyList list)
    {
        if (!CosmereKnownNames.IsEnabled)
        {
            base.AddNameProperties(list);
            return;
        }

        var name = _oplViewer != null ? GetNameFor(_oplViewer) : CosmereConfig.UnknownNameDisplay;
        name = string.IsNullOrEmpty(name) ? " " : name;

        string prefix;

        if (ShowFameTitle && (Player || Body.IsHuman) && Fame >= 10000)
        {
            prefix = Female ? "Lady " : "Lord ";
        }
        else
        {
            prefix = " ";
        }

        var guild = Guild;
        var hasTitle = PropertyTitle && !string.IsNullOrEmpty(Title);
        var hasGuild = guild != null && (Player || DisplayGuildTitle);

        var suffix = hasTitle switch
        {
            true when hasGuild  => $" {Title} [{guild.Abbreviation.FixHtmlFormattable()}]",
            true                => $" {Title}",
            false when hasGuild => $" [{guild.Abbreviation.FixHtmlFormattable()}]",
            _                   => " "
        };

        list.Add(1050045, $"{prefix}\t{name}\t{ApplyNameSuffix(suffix)}"); // ~1_PREFIX~~2_NAME~~3_SUFFIX~

        if (guild != null && (DisplayGuildTitle || Player && guild.Type != GuildType.Regular))
        {
            var guildTitle = GuildTitle?.Trim() ?? "";

            if (guildTitle.Length > 0)
            {
                if (NewGuildDisplay)
                {
                    list.Add($"{guildTitle.FixHtmlFormattable()}, {guild.Name.FixHtmlFormattable()}");
                }
                else
                {
                    list.Add($"{guildTitle.FixHtmlFormattable()}, {guild.Name.FixHtmlFormattable()} Guild");
                }
            }
            else
            {
                list.Add(guild.Name.FixHtml());
            }
        }
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
