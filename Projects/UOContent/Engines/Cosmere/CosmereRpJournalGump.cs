using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public class CosmereRpJournalGump : DynamicGump
{
    private const int BtnSaveProfile = 1;
    private const int BtnSaveDiary = 2;
    private const int BtnSubmitEval = 3;
    private const int BtnDeleteDiaryBase = 1000;
    private const int BtnEditDiaryBase = 2000;
    private const int BtnDeleteEvalBase = 3000;

    private const int EntryStory = 0;
    private const int EntryGoals = 1;
    private const int EntryAspirations = 2;
    private const int EntryNotes = 3;
    private const int EntryDiaryTitle = 10;
    private const int EntryDiaryBody = 11;
    private const int EntryFeedback = 20;
    private const int EntryScore = 21;

    private readonly PlayerMobile _viewer;
    private readonly PlayerMobile _subject;
    private readonly bool _staffMode;
    private readonly int _editingDiaryId;

    public override bool Singleton => true;

    private CosmereRpJournalGump(PlayerMobile viewer, PlayerMobile subject, bool staffMode, int editingDiaryId)
        : base(40, 40)
    {
        _viewer = viewer;
        _subject = subject;
        _staffMode = staffMode;
        _editingDiaryId = editingDiaryId;
    }

    public static void DisplayTo(Mobile from, PlayerMobile subject = null, bool staffMode = false, int editingDiaryId = -1)
    {
        if (from?.NetState == null || from is not PlayerMobile viewer)
        {
            return;
        }

        if (!CosmereRpJournal.IsEnabled)
        {
            from.SendMessage("RP journal is disabled.");
            return;
        }

        subject ??= viewer;

        if (staffMode && viewer.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("You do not have permission to view that journal.");
            return;
        }

        if (!staffMode && subject != viewer)
        {
            from.SendMessage("You can only edit your own journal.");
            return;
        }

        subject.EnsureCosmereSheet();
        from.SendGump(new CosmereRpJournalGump(viewer, subject, staffMode, editingDiaryId));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var sheet = _subject.CosmereSheet;
        var owner = !_staffMode && _viewer == _subject;
        const int width = 560;
        const int height = 520;

        builder.AddPage(); // shared chrome
        builder.AddBackground(0, 0, width, height, 0x13BE);
        builder.AddImageTiled(10, 10, width - 20, 25, 0xA40);

        var title = _staffMode
            ? $"RP Journal — {_subject.Name} (staff)"
            : "RP Journal";
        builder.AddHtml(15, 12, width - 30, 20, $"<CENTER>{title.FixHtml()}</CENTER>");

        builder.AddButton(20, 42, 0xFAE, 0xFAF, 0, GumpButtonType.Page, 1);
        builder.AddLabel(55, 42, 0x480, "Profile");
        builder.AddButton(140, 42, 0xFAE, 0xFAF, 0, GumpButtonType.Page, 2);
        builder.AddLabel(175, 42, 0x480, "Diary");
        builder.AddButton(250, 42, 0xFAE, 0xFAF, 0, GumpButtonType.Page, 3);
        builder.AddLabel(285, 42, 0x480, "Scores");

        var aggregate = CosmereRpJournal.FormatAggregate(sheet);
        builder.AddLabel(380, 42, 0x481, $"Avg: {aggregate}");

        BuildProfilePage(ref builder, sheet, owner, width);
        BuildDiaryPage(ref builder, sheet, owner, width);
        BuildScoresPage(ref builder, sheet, width);
    }

    private void BuildProfilePage(ref DynamicGumpBuilder builder, CosmereSheet sheet, bool owner, int width)
    {
        builder.AddPage(1);
        var y = 70;

        AddField(ref builder, "Story", sheet.RpStory, EntryStory, owner, ref y, width, 60);
        AddField(ref builder, "Goals", sheet.RpGoals, EntryGoals, owner, ref y, width, 50);
        AddField(ref builder, "Aspirations", sheet.RpAspirations, EntryAspirations, owner, ref y, width, 50);
        AddField(ref builder, "Notes", sheet.RpNotes, EntryNotes, owner, ref y, width, 50);

        if (owner)
        {
            builder.AddButton(width / 2 - 40, 470, 0xF9, 0xF8, BtnSaveProfile);
            builder.AddLabel(width / 2 + 5, 470, 0x480, "Save profile");
        }
    }

    private static void AddField(
        ref DynamicGumpBuilder builder,
        string label,
        string value,
        int entryId,
        bool editable,
        ref int y,
        int width,
        int height
    )
    {
        builder.AddLabel(20, y, 0x480, label);
        y += 18;
        if (editable)
        {
            builder.AddBackground(20, y, width - 40, height, 0xBB8);
            builder.AddTextEntry(25, y + 2, width - 50, height - 4, 0x481, entryId, value ?? "");
        }
        else
        {
            var text = string.IsNullOrWhiteSpace(value) ? "(empty)" : value.FixHtml();
            builder.AddHtml(20, y, width - 40, height, text);
        }

        y += height + 8;
    }

    private void BuildDiaryPage(ref DynamicGumpBuilder builder, CosmereSheet sheet, bool owner, int width)
    {
        builder.AddPage(2);
        var y = 70;

        builder.AddLabel(20, y, 0x480, "Entries (newest first)");
        y += 20;

        var shown = 0;
        for (var i = sheet.RpDiary.Count - 1; i >= 0 && shown < 5; i--)
        {
            var entry = sheet.RpDiary[i];
            var line =
                $"#{entry.Id} {entry.Created:yyyy-MM-dd} — {(entry.Title ?? "").FixHtml()}";
            builder.AddHtml(20, y, width - 120, 18, line);
            if (owner)
            {
                builder.AddButton(width - 90, y, 0xFAB, 0xFAC, BtnEditDiaryBase + entry.Id);
                builder.AddButton(width - 55, y, 0xFB1, 0xFB2, BtnDeleteDiaryBase + entry.Id);
            }

            y += 18;
            var preview = entry.Body ?? "";
            if (preview.Length > 80)
            {
                preview = preview[..80] + "…";
            }

            builder.AddHtml(30, y, width - 60, 32, preview.FixHtml());
            y += 36;
            shown++;
        }

        if (sheet.RpDiary.Count == 0)
        {
            builder.AddLabel(20, y, 0x481, "No diary entries yet.");
            y += 24;
        }

        if (owner)
        {
            y = Math.Max(y, 300);
            var editTitle = "";
            var editBody = "";
            var saveLabel = "Add entry";
            if (_editingDiaryId > 0)
            {
                for (var i = 0; i < sheet.RpDiary.Count; i++)
                {
                    if (sheet.RpDiary[i].Id != _editingDiaryId)
                    {
                        continue;
                    }

                    editTitle = sheet.RpDiary[i].Title ?? "";
                    editBody = sheet.RpDiary[i].Body ?? "";
                    saveLabel = $"Save #{_editingDiaryId}";
                    break;
                }
            }

            builder.AddLabel(20, y, 0x480, "Title");
            y += 18;
            builder.AddBackground(20, y, width - 40, 24, 0xBB8);
            builder.AddTextEntry(25, y + 2, width - 50, 20, 0x481, EntryDiaryTitle, editTitle);
            y += 28;
            builder.AddLabel(20, y, 0x480, "Body");
            y += 18;
            builder.AddBackground(20, y, width - 40, 70, 0xBB8);
            builder.AddTextEntry(25, y + 2, width - 50, 66, 0x481, EntryDiaryBody, editBody);
            y += 78;
            builder.AddButton(20, y, 0xF9, 0xF8, BtnSaveDiary);
            builder.AddLabel(55, y, 0x480, saveLabel);
        }
    }

    private void BuildScoresPage(ref DynamicGumpBuilder builder, CosmereSheet sheet, int width)
    {
        builder.AddPage(3);
        var y = 70;
        var canDelete = _viewer.AccessLevel >= AccessLevel.Administrator;

        builder.AddLabel(20, y, 0x480, $"Aggregate: {CosmereRpJournal.FormatAggregate(sheet)}");
        y += 22;
        builder.AddLabel(20, y, 0x480, "Current scores");
        y += 18;

        var anyCurrent = false;
        for (var i = sheet.RpEvaluations.Count - 1; i >= 0; i--)
        {
            var eval = sheet.RpEvaluations[i];
            if (eval.Superseded)
            {
                continue;
            }

            anyCurrent = true;
            builder.AddHtml(
                20,
                y,
                width - 80,
                18,
                $"{eval.EvaluatorName.FixHtml()}: {eval.Score}/5 — {eval.Created:yyyy-MM-dd}"
            );
            if (canDelete)
            {
                builder.AddButton(width - 50, y, 0xFB1, 0xFB2, BtnDeleteEvalBase + eval.Id);
            }

            y += 18;
            if (!string.IsNullOrWhiteSpace(eval.Feedback))
            {
                builder.AddHtml(30, y, width - 60, 36, eval.Feedback.FixHtml());
                y += 38;
            }
        }

        if (!anyCurrent)
        {
            builder.AddLabel(20, y, 0x481, "No current evaluations.");
            y += 20;
        }

        y += 8;
        builder.AddLabel(20, y, 0x480, "History");
        y += 18;

        var hist = 0;
        for (var i = sheet.RpEvaluations.Count - 1; i >= 0 && hist < 6; i--)
        {
            var eval = sheet.RpEvaluations[i];
            var tag = eval.Superseded ? " [superseded]" : "";
            builder.AddHtml(
                20,
                y,
                width - 80,
                18,
                $"#{eval.Id} {eval.EvaluatorName.FixHtml()} {eval.Score}/5 {eval.Created:yyyy-MM-dd}{tag}"
            );
            if (canDelete)
            {
                builder.AddButton(width - 50, y, 0xFB1, 0xFB2, BtnDeleteEvalBase + eval.Id);
            }

            y += 18;
            hist++;
        }

        if (_staffMode)
        {
            y = Math.Max(y + 10, 340);
            builder.AddLabel(20, y, 0x480, "Staff evaluation");
            y += 18;
            builder.AddLabel(20, y, 0x481, "Score (0-5)");
            builder.AddBackground(110, y - 2, 40, 24, 0xBB8);
            builder.AddTextEntry(115, y, 30, 20, 0x481, EntryScore, "3");
            y += 28;
            builder.AddLabel(20, y, 0x481, "Feedback");
            y += 18;
            builder.AddBackground(20, y, width - 40, 60, 0xBB8);
            builder.AddTextEntry(25, y + 2, width - 50, 56, 0x481, EntryFeedback, "");
            y += 68;
            builder.AddButton(20, y, 0xF9, 0xF8, BtnSubmitEval);
            builder.AddLabel(55, y, 0x480, "Submit evaluation");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile viewer || viewer != _viewer)
        {
            return;
        }

        if (!CosmereRpJournal.IsEnabled)
        {
            return;
        }

        if (_subject.Deleted)
        {
            viewer.SendMessage("That character is no longer available.");
            return;
        }

        _subject.EnsureCosmereSheet();
        var sheet = _subject.CosmereSheet;
        var owner = !_staffMode && viewer == _subject;
        var button = info.ButtonID;

        if (button == 0)
        {
            return;
        }

        if (button == BtnSaveProfile)
        {
            if (!owner)
            {
                viewer.SendMessage("Only the owner can edit the profile.");
                return;
            }

            CosmereRpJournal.SetProfile(
                sheet,
                info.GetTextEntry(EntryStory),
                info.GetTextEntry(EntryGoals),
                info.GetTextEntry(EntryAspirations),
                info.GetTextEntry(EntryNotes)
            );
            viewer.SendMessage("RP profile saved.");
            DisplayTo(viewer, _subject, _staffMode);
            return;
        }

        if (button == BtnSaveDiary)
        {
            if (!owner)
            {
                viewer.SendMessage("Only the owner can edit the diary.");
                return;
            }

            var title = info.GetTextEntry(EntryDiaryTitle) ?? "";
            var body = info.GetTextEntry(EntryDiaryBody) ?? "";
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            {
                viewer.SendMessage("Diary entry cannot be empty.");
                DisplayTo(viewer, _subject, _staffMode, _editingDiaryId);
                return;
            }

            if (_editingDiaryId > 0)
            {
                if (!CosmereRpJournal.TryUpdateDiaryEntry(sheet, _editingDiaryId, title, body))
                {
                    viewer.SendMessage("Diary entry not found.");
                }
                else
                {
                    viewer.SendMessage("Diary entry updated.");
                }
            }
            else
            {
                CosmereRpJournal.AddDiaryEntry(sheet, title, body);
                viewer.SendMessage("Diary entry added.");
            }

            DisplayTo(viewer, _subject, _staffMode);
            return;
        }

        if (button == BtnSubmitEval)
        {
            if (!_staffMode || viewer.AccessLevel < AccessLevel.GameMaster)
            {
                viewer.SendMessage("You cannot submit evaluations.");
                return;
            }

            var scoreText = info.GetTextEntry(EntryScore) ?? "0";
            if (!int.TryParse(scoreText.Trim(), out var score))
            {
                viewer.SendMessage("Score must be an integer from 0 to 5.");
                DisplayTo(viewer, _subject, true);
                return;
            }

            CosmereRpJournal.SubmitEvaluation(sheet, viewer, score, info.GetTextEntry(EntryFeedback));
            viewer.SendMessage("Evaluation submitted.");
            DisplayTo(viewer, _subject, true);
            return;
        }

        if (button >= BtnDeleteEvalBase)
        {
            if (viewer.AccessLevel < AccessLevel.Administrator)
            {
                viewer.SendMessage("Only Administrators can delete evaluations.");
                return;
            }

            var id = button - BtnDeleteEvalBase;
            if (CosmereRpJournal.TryDeleteEvaluation(sheet, id))
            {
                viewer.SendMessage("Evaluation deleted.");
            }
            else
            {
                viewer.SendMessage("Evaluation not found.");
            }

            DisplayTo(viewer, _subject, _staffMode);
            return;
        }

        if (button >= BtnEditDiaryBase)
        {
            if (!owner)
            {
                return;
            }

            DisplayTo(viewer, _subject, false, button - BtnEditDiaryBase);
            return;
        }

        if (button >= BtnDeleteDiaryBase)
        {
            if (!owner)
            {
                return;
            }

            var id = button - BtnDeleteDiaryBase;
            if (CosmereRpJournal.TryDeleteDiaryEntry(sheet, id))
            {
                viewer.SendMessage("Diary entry deleted.");
            }

            DisplayTo(viewer, _subject, false);
        }
    }
}
