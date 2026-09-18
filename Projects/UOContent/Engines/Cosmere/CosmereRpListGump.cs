using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public class CosmereRpListGump : DynamicGump
{
    private const int PageSize = 10;
    private const int BtnFilter = 1;
    private const int BtnPrev = 2;
    private const int BtnNext = 3;
    private const int BtnOpenBase = 100;
    private const int EntryFilter = 1;

    private readonly PlayerMobile _staff;
    private readonly string _filter;
    private readonly int _page;
    private readonly List<PlayerMobile> _matches;

    public override bool Singleton => true;

    private CosmereRpListGump(PlayerMobile staff, string filter, int page, List<PlayerMobile> matches)
        : base(50, 50)
    {
        _staff = staff;
        _filter = filter ?? "";
        _page = Math.Max(0, page);
        _matches = matches;
    }

    public static void DisplayTo(Mobile from, string filter = "", int page = 0)
    {
        if (from?.NetState == null || from is not PlayerMobile staff)
        {
            return;
        }

        if (!CosmereRpJournal.IsEnabled)
        {
            from.SendMessage("RP journal is disabled.");
            return;
        }

        if (staff.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("You do not have access to the RP journal list.");
            return;
        }

        var matches = CollectOnlineWithJournal(filter);
        from.SendGump(new CosmereRpListGump(staff, filter, page, matches));
    }

    private static List<PlayerMobile> CollectOnlineWithJournal(string filter)
    {
        var list = new List<PlayerMobile>();
        var hasFilter = !string.IsNullOrWhiteSpace(filter);

        foreach (var state in NetState.Instances)
        {
            if (state.Mobile is not PlayerMobile pm || pm.Deleted)
            {
                continue;
            }

            pm.EnsureCosmereSheet();
            if (!CosmereRpJournal.HasJournalContent(pm.CosmereSheet))
            {
                continue;
            }

            if (hasFilter &&
                (pm.Name == null || pm.Name.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            list.Add(pm);
        }

        list.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        const int width = 420;
        const int height = 400;
        var pageCount = Math.Max(1, (_matches.Count + PageSize - 1) / PageSize);
        var page = Math.Clamp(_page, 0, pageCount - 1);
        var start = page * PageSize;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, "<CENTER>RP Journals (online)</CENTER>");

        builder.AddLabel(20, 45, 0x480, "Filter");
        builder.AddBackground(70, 43, 220, 24, 0xBB8);
        builder.AddTextEntry(75, 45, 210, 20, 0x481, EntryFilter, _filter);
        builder.AddButton(300, 43, 0xF9, 0xF8, BtnFilter);
        builder.AddLabel(335, 45, 0x480, "Go");

        builder.AddLabel(20, 75, 0x481, $"{_matches.Count} match(es) — page {page + 1}/{pageCount}");

        var y = 100;
        var shown = 0;
        for (var i = start; i < _matches.Count && shown < PageSize; i++)
        {
            var pm = _matches[i];
            var sheet = pm.CosmereSheet;
            var avg = CosmereRpJournal.FormatAggregate(sheet);
            builder.AddButton(20, y, 0xFAE, 0xFAF, BtnOpenBase + i);
            builder.AddHtml(55, y, width - 80, 18, $"{pm.Name.FixHtml()} — avg {avg}");
            y += 24;
            shown++;
        }

        if (_matches.Count == 0)
        {
            builder.AddLabel(20, y, 0x481, "No online players with journal content.");
        }

        if (page > 0)
        {
            builder.AddButton(20, 360, 0xFAE, 0xFAF, BtnPrev);
            builder.AddLabel(55, 360, 0x480, "Prev");
        }

        if (page < pageCount - 1)
        {
            builder.AddButton(120, 360, 0xFAE, 0xFAF, BtnNext);
            builder.AddLabel(155, 360, 0x480, "Next");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile staff || staff != _staff)
        {
            return;
        }

        if (staff.AccessLevel < AccessLevel.GameMaster || !CosmereRpJournal.IsEnabled)
        {
            return;
        }

        var button = info.ButtonID;
        if (button == 0)
        {
            return;
        }

        var filter = info.GetTextEntry(EntryFilter) ?? _filter;
        var pageCount = Math.Max(1, (_matches.Count + PageSize - 1) / PageSize);
        var page = Math.Clamp(_page, 0, pageCount - 1);

        if (button == BtnFilter)
        {
            DisplayTo(staff, filter, 0);
            return;
        }

        if (button == BtnPrev)
        {
            DisplayTo(staff, filter, Math.Max(0, page - 1));
            return;
        }

        if (button == BtnNext)
        {
            DisplayTo(staff, filter, page + 1);
            return;
        }

        if (button >= BtnOpenBase)
        {
            var index = button - BtnOpenBase;
            if (index < 0 || index >= _matches.Count)
            {
                DisplayTo(staff, filter, page);
                return;
            }

            var subject = _matches[index];
            if (subject.Deleted)
            {
                staff.SendMessage("That player is no longer online.");
                DisplayTo(staff, filter, page);
                return;
            }

            CosmereRpJournalGump.DisplayTo(staff, subject, staffMode: true);
        }
    }
}
