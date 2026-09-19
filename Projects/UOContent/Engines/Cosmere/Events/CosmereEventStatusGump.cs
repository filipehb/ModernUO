using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere.Events;

public class CosmereEventStatusGump : DynamicGump
{
    private const int BtnRefresh = 1;
    private const int BtnBack = 2;
    private const int BtnStopBase = 100;

    private readonly PlayerMobile _staff;
    private readonly CosmereEventInstance[] _running;

    public override bool Singleton => true;

    private CosmereEventStatusGump(PlayerMobile staff, CosmereEventInstance[] running) : base(50, 50)
    {
        _staff = staff;
        _running = running;
    }

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null || from is not PlayerMobile staff)
        {
            return;
        }

        if (staff.AccessLevel < AccessLevel.Counselor)
        {
            return;
        }

        var list = new System.Collections.Generic.List<CosmereEventInstance>();
        foreach (var inst in CosmereEventRuntime.GetRunning())
        {
            list.Add(inst);
        }

        from.SendGump(new CosmereEventStatusGump(staff, list.ToArray()));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        const int width = 480;
        const int height = 360;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, "<CENTER>Running Cosmere Events</CENTER>");

        builder.AddButton(20, 42, 0xF9, 0xF8, BtnRefresh);
        builder.AddLabel(55, 42, 0x480, "Refresh");
        builder.AddButton(140, 42, 0xF9, 0xF8, BtnBack);
        builder.AddLabel(175, 42, 0x480, "Back");

        var y = 75;
        if (_running.Length == 0)
        {
            builder.AddLabel(20, y, 0x481, "No running instances.");
        }

        for (var i = 0; i < _running.Length && i < 8; i++)
        {
            var inst = _running[i];
            var ends = inst.EndsUtc == DateTime.MinValue
                ? "—"
                : inst.EndsUtc.ToString("u");
            builder.AddHtml(
                20,
                y,
                width - 90,
                36,
                $"#{inst.InstanceId} {inst.DefinitionId.FixHtml()} phase={inst.PhaseId.FixHtml()} spawns={inst.OwnedSerials.Count} ends={ends}"
            );
            builder.AddButton(width - 55, y, 0xFB1, 0xFB2, BtnStopBase + i);
            y += 40;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile staff || staff != _staff)
        {
            return;
        }

        var button = info.ButtonID;
        if (button == 0)
        {
            return;
        }

        if (button == BtnRefresh)
        {
            DisplayTo(staff);
            return;
        }

        if (button == BtnBack)
        {
            CosmereEventListGump.DisplayTo(staff);
            return;
        }

        if (button >= BtnStopBase)
        {
            var index = button - BtnStopBase;
            if (index >= 0 && index < _running.Length)
            {
                CosmereEventRuntime.End(_running[index], cancelled: true, "Stopped from status gump");
                staff.SendMessage("Instance stopped.");
            }

            DisplayTo(staff);
        }
    }
}
