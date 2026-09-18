using System;
using Server;
using Server.Engines.Cosmere;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Cosmere;

[Collection("Sequential UOContent Tests")]
public class CosmereRpJournalTests
{
    [Fact]
    public void SubmitEvaluation_SupersedesPriorFromSameStaff()
    {
        var sheet = new CosmereSheet();
        var staff = CreateStaff("GM One");

        try
        {
            CosmereConfig.SetRpJournalEnabledForTests(true);

            var first = CosmereRpJournal.SubmitEvaluation(sheet, staff, 2, "ok");
            var second = CosmereRpJournal.SubmitEvaluation(sheet, staff, 5, "great");

            Assert.True(first.Superseded);
            Assert.False(second.Superseded);
            Assert.Equal(2, sheet.RpEvaluations.Count);
            Assert.True(CosmereRpJournal.TryGetAggregateScore(sheet, out var avg));
            Assert.Equal(5.0, avg);
        }
        finally
        {
            staff.Delete();
        }
    }

    [Fact]
    public void Aggregate_AveragesCurrentScoresOnly()
    {
        var sheet = new CosmereSheet();
        var a = CreateStaff("A");
        var b = CreateStaff("B");

        try
        {
            CosmereRpJournal.SubmitEvaluation(sheet, a, 2, "a1");
            CosmereRpJournal.SubmitEvaluation(sheet, a, 4, "a2");
            CosmereRpJournal.SubmitEvaluation(sheet, b, 5, "b1");

            Assert.True(CosmereRpJournal.TryGetAggregateScore(sheet, out var avg));
            Assert.Equal(4.5, avg);
            Assert.Equal("4.5", CosmereRpJournal.FormatAggregate(sheet));
        }
        finally
        {
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void DeleteEvaluation_RestoresPriorCurrent()
    {
        var sheet = new CosmereSheet();
        var staff = CreateStaff("GM");

        try
        {
            var first = CosmereRpJournal.SubmitEvaluation(sheet, staff, 3, "first");
            var second = CosmereRpJournal.SubmitEvaluation(sheet, staff, 1, "second");

            Assert.True(CosmereRpJournal.TryDeleteEvaluation(sheet, second.Id));
            Assert.Single(sheet.RpEvaluations);
            Assert.False(first.Superseded);
            Assert.True(CosmereRpJournal.TryGetAggregateScore(sheet, out var avg));
            Assert.Equal(3.0, avg);
        }
        finally
        {
            staff.Delete();
        }
    }

    [Fact]
    public void HasJournalContent_DetectsProfileAndDiary()
    {
        var empty = new CosmereSheet();
        Assert.False(CosmereRpJournal.HasJournalContent(empty));

        empty.RpStory = "Once";
        Assert.True(CosmereRpJournal.HasJournalContent(empty));

        var diaryOnly = new CosmereSheet();
        CosmereRpJournal.AddDiaryEntry(diaryOnly, "Day 1", "Hello");
        Assert.True(CosmereRpJournal.HasJournalContent(diaryOnly));
    }

    [Fact]
    public void Diary_AddUpdateDelete()
    {
        var sheet = new CosmereSheet();
        var entry = CosmereRpJournal.AddDiaryEntry(sheet, "Title", "Body");
        Assert.Equal(1, entry.Id);
        Assert.True(CosmereRpJournal.TryUpdateDiaryEntry(sheet, entry.Id, "T2", "B2"));
        Assert.Equal("T2", sheet.RpDiary[0].Title);
        Assert.True(CosmereRpJournal.TryDeleteDiaryEntry(sheet, entry.Id));
        Assert.Empty(sheet.RpDiary);
    }

    [Fact]
    public void FormatAggregate_EmptyIsEmDash()
    {
        Assert.Equal("—", CosmereRpJournal.FormatAggregate(new CosmereSheet()));
    }

    private static PlayerMobile CreateStaff(string name)
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        pm.Name = name;
        pm.AccessLevel = AccessLevel.GameMaster;
        return pm;
    }
}
