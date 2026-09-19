using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Cosmere;

public class CosmereKnownNameGump : DynamicGump
{
    private const int BtnSave = 1;
    private const int BtnForget = 2;
    private const int EntryLabel = 0;
    private const int EntryNotes = 1;

    private readonly PlayerMobile _viewer;
    private readonly PlayerMobile _subject;
    private readonly string _initialLabel;
    private readonly string _initialNotes;

    public override bool Singleton => true;

    private CosmereKnownNameGump(
        PlayerMobile viewer,
        PlayerMobile subject,
        string label,
        string notes
    ) : base(50, 50)
    {
        _viewer = viewer;
        _subject = subject;
        _initialLabel = label ?? "";
        _initialNotes = notes ?? "";
    }

    public static void DisplayTo(PlayerMobile viewer, PlayerMobile subject)
    {
        if (viewer?.NetState == null || subject == null || subject.Deleted)
        {
            return;
        }

        if (!CosmereKnownNames.IsEnabled)
        {
            viewer.SendMessage("Known names are disabled.");
            return;
        }

        CosmereKnownNames.TryGetEntry(viewer, subject.Serial, out var entry);
        viewer.SendGump(
            new CosmereKnownNameGump(
                viewer,
                subject,
                entry?.Label ?? "",
                entry?.Notes ?? ""
            )
        );
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        const int width = 420;
        const int height = 280;

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);
        builder.AddHtml(15, 12, width - 30, 20, "<CENTER>Recognize</CENTER>");

        builder.AddLabel(20, 45, 0x480, "Who you think they are (max 25)");
        builder.AddBackground(20, 65, width - 40, 24, 0xBB8);
        builder.AddTextEntryLimited(25, 67, width - 50, 20, 0x481, EntryLabel, _initialLabel, CosmereKnownNames.MaxLabelLength);

        builder.AddLabel(20, 100, 0x480, "Notes (free text)");
        builder.AddBackground(20, 120, width - 40, 80, 0xBB8);
        builder.AddTextEntry(25, 122, width - 50, 76, 0x481, EntryNotes, _initialNotes);

        builder.AddButton(20, 220, 0xF9, 0xF8, BtnSave);
        builder.AddLabel(55, 220, 0x480, "Save");
        builder.AddButton(140, 220, 0xFB1, 0xFB2, BtnForget);
        builder.AddLabel(175, 220, 0x480, "Forget");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile viewer || viewer != _viewer)
        {
            return;
        }

        if (_subject.Deleted)
        {
            viewer.SendMessage("That person is no longer around.");
            return;
        }

        if (info.ButtonID == 0)
        {
            return;
        }

        if (info.ButtonID == BtnForget)
        {
            CosmereKnownNames.Remove(viewer, _subject.Serial);
            viewer.SendMessage("You no longer recognize them.");
            _subject.Delta(MobileDelta.Name);
            return;
        }

        if (info.ButtonID == BtnSave)
        {
            var label = info.GetTextEntry(EntryLabel) ?? "";
            var notes = info.GetTextEntry(EntryNotes) ?? "";
            CosmereKnownNames.Set(viewer, _subject.Serial, label, notes);

            if (string.IsNullOrWhiteSpace(label))
            {
                viewer.SendMessage("Cleared — they are a stranger again.");
            }
            else
            {
                viewer.SendMessage($"You will know them as '{CosmereKnownNames.TruncateLabel(label)}'.");
            }

            _subject.Delta(MobileDelta.Name);
        }
    }
}

public class CosmereRecognizeTarget : Target
{
    private readonly PlayerMobile _from;

    public CosmereRecognizeTarget(PlayerMobile from) : base(12, false, TargetFlags.None) => _from = from;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (from != _from)
        {
            return;
        }

        if (targeted is not PlayerMobile subject || subject.Deleted)
        {
            _from.SendMessage("You can only recognize other players.");
            return;
        }

        if (subject == _from)
        {
            _from.SendMessage("You already know yourself.");
            return;
        }

        CosmereKnownNameGump.DisplayTo(_from, subject);
    }
}
