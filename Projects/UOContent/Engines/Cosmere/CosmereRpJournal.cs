using System;
using System.Globalization;

namespace Server.Engines.Cosmere;

public static class CosmereRpJournal
{
    public const int MaxProfileLength = 2048;
    public const int MaxDiaryTitleLength = 128;
    public const int MaxDiaryBodyLength = 2048;
    public const int MaxFeedbackLength = 1024;
    public const int MinScore = 0;
    public const int MaxScore = 5;

    public static bool IsEnabled => CosmereConfig.RpJournalEnabled;

    public static bool HasJournalContent(CosmereSheet sheet)
    {
        if (sheet == null)
        {
            return false;
        }

        if (sheet.RpDiary.Count > 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(sheet.RpStory)
               || !string.IsNullOrWhiteSpace(sheet.RpGoals)
               || !string.IsNullOrWhiteSpace(sheet.RpAspirations)
               || !string.IsNullOrWhiteSpace(sheet.RpNotes);
    }

    public static void SetProfile(
        CosmereSheet sheet,
        string story,
        string goals,
        string aspirations,
        string notes
    )
    {
        ArgumentNullException.ThrowIfNull(sheet);

        sheet.RpStory = Truncate(story, MaxProfileLength);
        sheet.RpGoals = Truncate(goals, MaxProfileLength);
        sheet.RpAspirations = Truncate(aspirations, MaxProfileLength);
        sheet.RpNotes = Truncate(notes, MaxProfileLength);
    }

    public static CosmereRpDiaryEntry AddDiaryEntry(CosmereSheet sheet, string title, string body)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var entry = new CosmereRpDiaryEntry
        {
            Id = sheet.NextRpDiaryId++,
            Created = DateTime.UtcNow,
            Title = Truncate(title, MaxDiaryTitleLength),
            Body = Truncate(body, MaxDiaryBodyLength)
        };

        sheet.RpDiary.Add(entry);
        return entry;
    }

    public static bool TryUpdateDiaryEntry(CosmereSheet sheet, int id, string title, string body)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        for (var i = 0; i < sheet.RpDiary.Count; i++)
        {
            if (sheet.RpDiary[i].Id != id)
            {
                continue;
            }

            sheet.RpDiary[i].Title = Truncate(title, MaxDiaryTitleLength);
            sheet.RpDiary[i].Body = Truncate(body, MaxDiaryBodyLength);
            return true;
        }

        return false;
    }

    public static bool TryDeleteDiaryEntry(CosmereSheet sheet, int id)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        for (var i = 0; i < sheet.RpDiary.Count; i++)
        {
            if (sheet.RpDiary[i].Id != id)
            {
                continue;
            }

            sheet.RpDiary.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static CosmereRpEvaluation SubmitEvaluation(
        CosmereSheet sheet,
        Mobile evaluator,
        int score,
        string feedback
    )
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(evaluator);

        score = Math.Clamp(score, MinScore, MaxScore);
        var serial = evaluator.Serial;

        for (var i = 0; i < sheet.RpEvaluations.Count; i++)
        {
            var existing = sheet.RpEvaluations[i];
            if (existing.EvaluatorSerial == serial && !existing.Superseded)
            {
                existing.Superseded = true;
            }
        }

        var eval = new CosmereRpEvaluation
        {
            Id = sheet.NextRpEvalId++,
            EvaluatorSerial = serial,
            EvaluatorName = evaluator.RawName ?? evaluator.Name ?? "Staff",
            Created = DateTime.UtcNow,
            Score = score,
            Feedback = Truncate(feedback, MaxFeedbackLength),
            Superseded = false
        };

        sheet.RpEvaluations.Add(eval);
        return eval;
    }

    public static bool TryDeleteEvaluation(CosmereSheet sheet, int id)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        Serial? evaluator = null;
        for (var i = 0; i < sheet.RpEvaluations.Count; i++)
        {
            if (sheet.RpEvaluations[i].Id != id)
            {
                continue;
            }

            evaluator = sheet.RpEvaluations[i].EvaluatorSerial;
            sheet.RpEvaluations.RemoveAt(i);
            break;
        }

        if (evaluator == null)
        {
            return false;
        }

        RecomputeSuperseded(sheet, evaluator.Value);
        return true;
    }

    public static void RecomputeSuperseded(CosmereSheet sheet, Serial evaluatorSerial)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        CosmereRpEvaluation latest = null;
        for (var i = 0; i < sheet.RpEvaluations.Count; i++)
        {
            var eval = sheet.RpEvaluations[i];
            if (eval.EvaluatorSerial != evaluatorSerial)
            {
                continue;
            }

            eval.Superseded = true;
            if (latest == null || eval.Created > latest.Created ||
                eval.Created == latest.Created && eval.Id > latest.Id)
            {
                latest = eval;
            }
        }

        if (latest != null)
        {
            latest.Superseded = false;
        }
    }

    public static bool TryGetAggregateScore(CosmereSheet sheet, out double average)
    {
        average = 0;
        if (sheet == null)
        {
            return false;
        }

        var sum = 0;
        var count = 0;
        for (var i = 0; i < sheet.RpEvaluations.Count; i++)
        {
            if (sheet.RpEvaluations[i].Superseded)
            {
                continue;
            }

            sum += sheet.RpEvaluations[i].Score;
            count++;
        }

        if (count == 0)
        {
            return false;
        }

        average = sum / (double)count;
        return true;
    }

    public static string FormatAggregate(CosmereSheet sheet) =>
        TryGetAggregateScore(sheet, out var average)
            ? average.ToString("0.0", CultureInfo.InvariantCulture)
            : "—";

    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        value = value.Trim();
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
