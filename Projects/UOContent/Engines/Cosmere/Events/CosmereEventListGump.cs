using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere.Events;

public class CosmereEventListGump : DynamicGump
{
    private const int PageSize = 8;
    private const int BtnRefresh = 1;
    private const int BtnCreate = 2;
    private const int BtnStatus = 3;
    private const int BtnPrev = 4;
    private const int BtnNext = 5;
    private const int BtnOpenBase = 100;
    private const int BtnStartBase = 200;
    private const int BtnStopBase = 300;

    private readonly PlayerMobile _staff;
    private readonly int _page;
    private readonly string[] _ids;

    public override bool Singleton => true;

    private CosmereEventListGump(PlayerMobile staff, int page, string[] ids) : base(40, 40)
    {
        _staff = staff;
        _page = Math.Max(0, page);
        _ids = ids;
    }

    public static void DisplayTo(Mobile from, int page = 0)
    {
        if (from?.NetState == null || from is not PlayerMobile staff)
        {
            return;
        }

        if (staff.AccessLevel < AccessLevel.Counselor)
        {
            from.SendMessage("Cosmere events require Counselor access.");
            return;
        }

        if (!CosmereConfig.EventsEnabled)
        {
            from.SendMessage("Cosmere events are disabled.");
            return;
        }

        var ids = new string[CosmereEventCatalog.Definitions.Count];
        var i = 0;
        foreach (var pair in CosmereEventCatalog.Definitions)
        {
            ids[i++] = pair.Key;
        }

        Array.Sort(ids, StringComparer.OrdinalIgnoreCase);
        from.SendGump(new CosmereEventListGump(staff, page, ids));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        const int width = 520;
        const int height = 420;
        var pageCount = Math.Max(1, (_ids.Length + PageSize - 1) / PageSize);
        var page = Math.Clamp(_page, 0, pageCount - 1);
        var start = page * PageSize;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, "<CENTER>Cosmere Events</CENTER>");

        builder.AddButton(20, 42, 0xF9, 0xF8, BtnRefresh);
        builder.AddLabel(55, 42, 0x480, "Refresh");
        builder.AddButton(140, 42, 0xF9, 0xF8, BtnCreate);
        builder.AddLabel(175, 42, 0x480, "New");
        builder.AddButton(240, 42, 0xF9, 0xF8, BtnStatus);
        builder.AddLabel(275, 42, 0x480, "Running");

        builder.AddLabel(20, 70, 0x481, $"{_ids.Length} definition(s) — page {page + 1}/{pageCount}");

        var y = 95;
        for (var i = start; i < _ids.Length && i < start + PageSize; i++)
        {
            var id = _ids[i];
            CosmereEventCatalog.TryGet(id, out var def);
            var running = CosmereEventRuntime.CountRunning(id);
            var title = def?.Title ?? id;
            var enabled = def?.Enabled == true ? "" : " [off]";
            builder.AddButton(20, y, 0xFAE, 0xFAF, BtnOpenBase + i);
            builder.AddHtml(55, y, 280, 18, $"{title.FixHtml()} ({id}){enabled} run:{running}");
            builder.AddButton(350, y, 0xFAB, 0xFAC, BtnStartBase + i);
            builder.AddLabel(385, y, 0x480, "Start");
            builder.AddButton(430, y, 0xFB1, 0xFB2, BtnStopBase + i);
            builder.AddLabel(465, y, 0x480, "Stop");
            y += 28;
        }

        if (_ids.Length == 0)
        {
            builder.AddLabel(20, y, 0x481, "No definitions. Use New or drop JSON in Configuration/Cosmere/Events.");
        }

        if (page > 0)
        {
            builder.AddButton(20, 380, 0xFAE, 0xFAF, BtnPrev);
            builder.AddLabel(55, 380, 0x480, "Prev");
        }

        if (page < pageCount - 1)
        {
            builder.AddButton(120, 380, 0xFAE, 0xFAF, BtnNext);
            builder.AddLabel(155, 380, 0x480, "Next");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile staff || staff != _staff)
        {
            return;
        }

        if (staff.AccessLevel < AccessLevel.Counselor || !CosmereConfig.EventsEnabled)
        {
            return;
        }

        var button = info.ButtonID;
        if (button == 0)
        {
            return;
        }

        var pageCount = Math.Max(1, (_ids.Length + PageSize - 1) / PageSize);
        var page = Math.Clamp(_page, 0, pageCount - 1);

        if (button == BtnRefresh)
        {
            DisplayTo(staff, page);
            return;
        }

        if (button == BtnCreate)
        {
            CosmereEventEditorGump.DisplayNew(staff);
            return;
        }

        if (button == BtnStatus)
        {
            CosmereEventStatusGump.DisplayTo(staff);
            return;
        }

        if (button == BtnPrev)
        {
            DisplayTo(staff, Math.Max(0, page - 1));
            return;
        }

        if (button == BtnNext)
        {
            DisplayTo(staff, page + 1);
            return;
        }

        if (button >= BtnStopBase)
        {
            var index = button - BtnStopBase;
            if (index >= 0 && index < _ids.Length)
            {
                CosmereEventRuntime.TryStopDefinition(_ids[index], cancel: true, out var stopped);
                staff.SendMessage(stopped > 0 ? $"Stopped {stopped} instance(s)." : "No running instances.");
            }

            DisplayTo(staff, page);
            return;
        }

        if (button >= BtnStartBase)
        {
            var index = button - BtnStartBase;
            if (index >= 0 && index < _ids.Length)
            {
                if (CosmereEventRuntime.TryStart(_ids[index], out var inst, out var error))
                {
                    staff.SendMessage($"Started {_ids[index]} (instance {inst.InstanceId}).");
                }
                else
                {
                    staff.SendMessage(error ?? "Start failed.");
                }
            }

            DisplayTo(staff, page);
            return;
        }

        if (button >= BtnOpenBase)
        {
            var index = button - BtnOpenBase;
            if (index >= 0 && index < _ids.Length)
            {
                CosmereEventEditorGump.DisplayTo(staff, _ids[index]);
            }
        }
    }
}
