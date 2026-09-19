using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere.Events;

public class CosmereEventEditorGump : DynamicGump
{
    private const int BtnSave = 1;
    private const int BtnBack = 2;
    private const int BtnToggleEnabled = 3;
    private const int BtnToggleScope = 4;
    private const int EntryId = 0;
    private const int EntryTitle = 1;
    private const int EntrySummary = 2;
    private const int EntryMap = 3;
    private const int EntryX = 4;
    private const int EntryY = 5;
    private const int EntryW = 6;
    private const int EntryH = 7;
    private const int EntryDuration = 8;
    private const int EntryAnnounce = 9;

    private readonly PlayerMobile _staff;
    private readonly CosmereEventDefinition _draft;
    private readonly bool _isNew;

    public override bool Singleton => true;

    private CosmereEventEditorGump(PlayerMobile staff, CosmereEventDefinition draft, bool isNew) : base(40, 40)
    {
        _staff = staff;
        _draft = draft;
        _isNew = isNew;
    }

    public static void DisplayTo(Mobile from, string definitionId)
    {
        if (from is not PlayerMobile staff || staff.AccessLevel < AccessLevel.Counselor)
        {
            return;
        }

        if (!CosmereEventCatalog.TryGet(definitionId, out var def))
        {
            from.SendMessage("Definition not found.");
            return;
        }

        // Work on a shallow-enough copy for meta edits
        var draft = CloneMeta(def);
        from.SendGump(new CosmereEventEditorGump(staff, draft, isNew: false));
    }

    public static void DisplayNew(Mobile from)
    {
        if (from is not PlayerMobile staff || staff.AccessLevel < AccessLevel.Counselor)
        {
            return;
        }

        var draft = CosmereEventCatalog.CreateBlank($"event-{Utility.Random(100000):D5}");
        from.SendGump(new CosmereEventEditorGump(staff, draft, isNew: true));
    }

    private static CosmereEventDefinition CloneMeta(CosmereEventDefinition src) =>
        new()
        {
            SchemaVersion = src.SchemaVersion,
            Id = src.Id,
            Title = src.Title,
            Summary = src.Summary,
            StaffNotes = src.StaffNotes,
            Scope = src.Scope,
            Map = src.Map,
            X = src.X,
            Y = src.Y,
            Width = src.Width,
            Height = src.Height,
            Enabled = src.Enabled,
            StartMode = src.StartMode,
            EndMode = src.EndMode,
            DurationMinutes = src.DurationMinutes,
            DeadlineUtc = src.DeadlineUtc,
            MaxConcurrentInstances = src.MaxConcurrentInstances,
            StartTriggers = src.StartTriggers,
            Phases = src.Phases
        };

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        const int width = 520;
        const int height = 480;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, $"<CENTER>{(_isNew ? "New" : "Edit")} Event</CENTER>");

        var y = 45;
        builder.AddLabel(20, y, 0x480, "Id");
        builder.AddBackground(80, y - 2, 200, 24, 0xBB8);
        builder.AddTextEntry(85, y, 190, 20, 0x481, EntryId, _draft.Id ?? "");
        y += 28;

        builder.AddLabel(20, y, 0x480, "Title");
        builder.AddBackground(80, y - 2, 400, 24, 0xBB8);
        builder.AddTextEntry(85, y, 390, 20, 0x481, EntryTitle, _draft.Title ?? "");
        y += 28;

        builder.AddLabel(20, y, 0x480, "Summary");
        builder.AddBackground(80, y - 2, 400, 40, 0xBB8);
        builder.AddTextEntry(85, y, 390, 36, 0x481, EntrySummary, _draft.Summary ?? "");
        y += 48;

        builder.AddButton(20, y, 0xFAE, 0xFAF, BtnToggleEnabled);
        builder.AddLabel(55, y, 0x481, _draft.Enabled ? "Enabled" : "Disabled");
        builder.AddButton(160, y, 0xFAE, 0xFAF, BtnToggleScope);
        builder.AddLabel(195, y, 0x481, $"Scope: {_draft.Scope}");
        y += 28;

        builder.AddLabel(20, y, 0x480, "Map");
        builder.AddBackground(80, y - 2, 120, 24, 0xBB8);
        builder.AddTextEntry(85, y, 110, 20, 0x481, EntryMap, _draft.Map ?? "Felucca");
        builder.AddLabel(220, y, 0x480, "X");
        builder.AddBackground(240, y - 2, 50, 24, 0xBB8);
        builder.AddTextEntry(245, y, 40, 20, 0x481, EntryX, $"{_draft.X}");
        builder.AddLabel(300, y, 0x480, "Y");
        builder.AddBackground(320, y - 2, 50, 24, 0xBB8);
        builder.AddTextEntry(325, y, 40, 20, 0x481, EntryY, $"{_draft.Y}");
        y += 28;

        builder.AddLabel(20, y, 0x480, "W");
        builder.AddBackground(50, y - 2, 50, 24, 0xBB8);
        builder.AddTextEntry(55, y, 40, 20, 0x481, EntryW, $"{_draft.Width}");
        builder.AddLabel(110, y, 0x480, "H");
        builder.AddBackground(130, y - 2, 50, 24, 0xBB8);
        builder.AddTextEntry(135, y, 40, 20, 0x481, EntryH, $"{_draft.Height}");
        builder.AddLabel(200, y, 0x480, "Duration min");
        builder.AddBackground(300, y - 2, 60, 24, 0xBB8);
        builder.AddTextEntry(
            305,
            y,
            50,
            20,
            0x481,
            EntryDuration,
            _draft.DurationMinutes?.ToString() ?? ""
        );
        y += 32;

        builder.AddLabel(20, y, 0x480, "Phases (JSON for advanced triggers/actions)");
        y += 20;
        var phaseLines = 0;
        for (var i = 0; i < _draft.Phases.Count && phaseLines < 4; i++)
        {
            var p = _draft.Phases[i];
            builder.AddHtml(30, y, width - 60, 18, $"- {p.Id.FixHtml()}: {(p.Title ?? "").FixHtml()}");
            y += 18;
            phaseLines++;
        }

        y += 8;
        builder.AddLabel(20, y, 0x480, "First OnEnter Announce (quick edit)");
        y += 20;
        var announce = "";
        if (_draft.Phases.Count > 0 && _draft.Phases[0].OnEnter != null)
        {
            for (var i = 0; i < _draft.Phases[0].OnEnter.Count; i++)
            {
                if (string.Equals(_draft.Phases[0].OnEnter[i].Type, "Announce", StringComparison.OrdinalIgnoreCase))
                {
                    announce = _draft.Phases[0].OnEnter[i].Message ?? "";
                    break;
                }
            }
        }

        builder.AddBackground(20, y, width - 40, 50, 0xBB8);
        builder.AddTextEntry(25, y + 2, width - 50, 46, 0x481, EntryAnnounce, announce);

        builder.AddButton(20, 440, 0xF9, 0xF8, BtnSave);
        builder.AddLabel(55, 440, 0x480, "Save JSON");
        builder.AddButton(160, 440, 0xF2, 0xF1, BtnBack);
        builder.AddLabel(195, 440, 0x480, "Back");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile staff || staff != _staff)
        {
            return;
        }

        if (staff.AccessLevel < AccessLevel.Counselor)
        {
            return;
        }

        var button = info.ButtonID;
        if (button == 0)
        {
            return;
        }

        ApplyFields(info);

        if (button == BtnBack)
        {
            CosmereEventListGump.DisplayTo(staff);
            return;
        }

        if (button == BtnToggleEnabled)
        {
            _draft.Enabled = !_draft.Enabled;
            staff.SendGump(new CosmereEventEditorGump(staff, _draft, _isNew));
            return;
        }

        if (button == BtnToggleScope)
        {
            _draft.Scope = _draft.Scope == CosmereEventScope.Global
                ? CosmereEventScope.Regional
                : CosmereEventScope.Global;
            staff.SendGump(new CosmereEventEditorGump(staff, _draft, _isNew));
            return;
        }

        if (button == BtnSave)
        {
            if (_draft.EndMode == CosmereEventEndMode.Manual && _draft.DurationMinutes is > 0)
            {
                _draft.EndMode = CosmereEventEndMode.Duration;
            }

            EnsureAnnouncePhase();
            if (!CosmereEventCatalog.Save(_draft, out var error))
            {
                staff.SendMessage(error ?? "Save failed.");
                staff.SendGump(new CosmereEventEditorGump(staff, _draft, _isNew));
                return;
            }

            staff.SendMessage($"Saved event '{_draft.Id}'.");
            CosmereEventRuntime.ReloadSchedules();
            CosmereEventListGump.DisplayTo(staff);
        }
    }

    private void ApplyFields(in RelayInfo info)
    {
        var id = info.GetTextEntry(EntryId)?.Trim();
        if (!string.IsNullOrWhiteSpace(id))
        {
            _draft.Id = id;
        }

        _draft.Title = info.GetTextEntry(EntryTitle)?.Trim() ?? _draft.Title;
        _draft.Summary = info.GetTextEntry(EntrySummary)?.Trim() ?? _draft.Summary;
        _draft.Map = info.GetTextEntry(EntryMap)?.Trim();

        if (int.TryParse(info.GetTextEntry(EntryX)?.Trim(), out var x))
        {
            _draft.X = x;
        }

        if (int.TryParse(info.GetTextEntry(EntryY)?.Trim(), out var y))
        {
            _draft.Y = y;
        }

        if (int.TryParse(info.GetTextEntry(EntryW)?.Trim(), out var w))
        {
            _draft.Width = w;
        }

        if (int.TryParse(info.GetTextEntry(EntryH)?.Trim(), out var h))
        {
            _draft.Height = h;
        }

        var durText = info.GetTextEntry(EntryDuration)?.Trim();
        if (string.IsNullOrWhiteSpace(durText))
        {
            _draft.DurationMinutes = null;
        }
        else if (double.TryParse(durText, out var dur))
        {
            _draft.DurationMinutes = dur;
        }

        var announce = info.GetTextEntry(EntryAnnounce);
        if (announce != null)
        {
            EnsureAnnouncePhase();
            var onEnter = _draft.Phases[0].OnEnter;
            CosmereEventActionDef announceAction = null;
            for (var i = 0; i < onEnter.Count; i++)
            {
                if (string.Equals(onEnter[i].Type, "Announce", StringComparison.OrdinalIgnoreCase))
                {
                    announceAction = onEnter[i];
                    break;
                }
            }

            if (announceAction == null)
            {
                announceAction = new CosmereEventActionDef { Type = "Announce", Audience = "All" };
                onEnter.Insert(0, announceAction);
            }

            announceAction.Message = announce;
        }
    }

    private void EnsureAnnouncePhase()
    {
        if (_draft.Phases.Count == 0)
        {
            _draft.Phases.Add(new CosmereEventPhaseDef { Id = "main", Title = "Main" });
        }

        _draft.Phases[0].OnEnter ??= [];
    }
}
