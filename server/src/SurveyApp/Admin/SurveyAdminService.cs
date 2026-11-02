using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SurveyApp.Data;

namespace SurveyApp.Admin;

// 運営ツール（Blazor）から使う、アンケートの作成・公開・集計の処理。
// Blazor Server では 1 つの接続が長く続くため、DbContext は操作ごとに作って使い捨てる。
public class SurveyAdminService(IDbContextFactory<ApplicationDbContext> dbFactory, TimeProvider time)
{
    public const int QuestionTextMaxLength = Question.TextMaxLength;
    public const int ChoiceTextMaxLength = Choice.TextMaxLength;

    // 運営は日本で行う前提で、締め切りの入力と表示は日本時間にする
    public static readonly TimeZoneInfo OpsTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    public async Task<List<SurveySummary>> ListAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Surveys
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SurveySummary(s.Id, s.Title, s.Status, s.ClosesAt, s.Questions.Count, s.Responses.Count, s.UpdatedAt))
            .ToListAsync();
    }

    public async Task<Survey?> GetAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await LoadWithQuestions(db).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public static SurveyForm ToForm(Survey survey) => new()
    {
        Title = survey.Title,
        Description = survey.Description,
        ClosesAtLocal = survey.ClosesAt is { } closesAt ? TimeZoneInfo.ConvertTime(closesAt, OpsTimeZone).DateTime : null,
        Questions = survey.Questions.OrderBy(q => q.Order).Select(q => new QuestionForm
        {
            Text = q.Text,
            Type = q.Type,
            IsRequired = q.IsRequired,
            Choices = q.HasChoices ? q.Choices.OrderBy(c => c.Order).Select(c => new ChoiceForm { Text = c.Text }).ToList() : [new(), new()],
        }).ToList(),
    };

    /// <summary>
    /// 下書きを新規作成、または上書き保存します。公開後のアンケートは回答の集計が崩れるので編集できません。
    /// </summary>
    public async Task<(Guid? Id, List<string> Errors)> SaveDraftAsync(Guid? id, SurveyForm form)
    {
        var errors = Validate(form);
        if (errors.Count > 0)
        {
            return (null, errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var now = time.GetUtcNow();

        Survey survey;
        if (id is null)
        {
            survey = new Survey { Title = "", CreatedAt = now };
            db.Surveys.Add(survey);
        }
        else
        {
            var existing = await LoadWithQuestions(db).FirstOrDefaultAsync(s => s.Id == id);
            if (existing is null)
            {
                return (null, ["アンケートが見つかりません。"]);
            }

            if (existing.Status != SurveyStatus.Draft)
            {
                return (null, ["公開後のアンケートは編集できません。"]);
            }

            // 質問は並び替えや削除があるので、丸ごと作り直す（下書きには回答がないので安全）
            db.Questions.RemoveRange(existing.Questions);
            existing.Questions.Clear();
            survey = existing;
        }

        Apply(form, survey);
        survey.UpdatedAt = now;
        await db.SaveChangesAsync();
        return (survey.Id, []);
    }

    /// <summary>下書きを公開し、ゲームから回答できるようにします。問題があればエラーメッセージを返します。</summary>
    public async Task<string?> PublishAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var survey = await LoadWithQuestions(db).FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return "アンケートが見つかりません。";
        }

        var now = time.GetUtcNow();
        if (survey.Status != SurveyStatus.Draft)
        {
            return "下書きのアンケートだけ公開できます。";
        }

        if (survey.Questions.Count == 0)
        {
            return "質問を 1 つ以上追加してください。";
        }

        if (survey.ClosesAt <= now)
        {
            return "締め切りが過ぎています。締め切りを変更してから公開してください。";
        }

        survey.Status = SurveyStatus.Published;
        survey.UpdatedAt = now;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task CloseAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var survey = await db.Surveys.FirstOrDefaultAsync(s => s.Id == id && s.Status == SurveyStatus.Published);
        if (survey is null)
        {
            return;
        }

        survey.Status = SurveyStatus.Closed;
        survey.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync();
    }

    /// <summary>下書きだけ削除できます（公開後は回答が消えてしまうため）。</summary>
    public async Task<bool> DeleteDraftAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Surveys.Where(s => s.Id == id && s.Status == SurveyStatus.Draft).ExecuteDeleteAsync() > 0;
    }

    public async Task<SurveyResults?> GetResultsAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var survey = await LoadWithQuestions(db).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return null;
        }

        var answers = db.Answers.Where(a => a.Response!.SurveyId == id);

        // 集計は DB 側で行い、選択肢ごとの件数と質問ごとの回答人数だけを受け取る
        var choiceCounts = await answers
            .Where(a => a.ChoiceId != null)
            .GroupBy(a => a.ChoiceId!.Value)
            .Select(g => new { ChoiceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChoiceId, x => x.Count);

        var answeredCounts = await answers
            .GroupBy(a => a.QuestionId)
            .Select(g => new { QuestionId = g.Key, Count = g.Select(a => a.ResponseId).Distinct().Count() })
            .ToDictionaryAsync(x => x.QuestionId, x => x.Count);

        var textAnswers = (await answers
            .Where(a => a.Text != null)
            .OrderByDescending(a => a.Response!.SubmittedAt)
            .Select(a => new { a.QuestionId, Text = a.Text! })
            .ToListAsync())
            .ToLookup(a => a.QuestionId, a => a.Text);

        var responseCount = await db.Responses.CountAsync(r => r.SurveyId == id);

        var questions = survey.Questions.OrderBy(q => q.Order).Select(q => new QuestionResult(
            q.Order,
            q.Text,
            q.Type,
            q.IsRequired,
            answeredCounts.GetValueOrDefault(q.Id),
            q.Choices.OrderBy(c => c.Order).Select(c => new ChoiceResult(c.Text, choiceCounts.GetValueOrDefault(c.Id))).ToList(),
            textAnswers[q.Id].ToList())).ToList();

        return new SurveyResults(survey.Id, survey.Title, survey.Description, survey.Status, survey.ClosesAt, responseCount, questions);
    }

    /// <summary>
    /// 回答を 1 回答 1 行の CSV にします。Excel で文字化けしないよう UTF-8 の BOM を付けます。
    /// </summary>
    public async Task<(string FileName, byte[] Content)?> ExportCsvAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var survey = await LoadWithQuestions(db).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return null;
        }

        var questions = survey.Questions.OrderBy(q => q.Order).ToList();
        var choiceTexts = questions.SelectMany(q => q.Choices).ToDictionary(c => c.Id, c => c.Text);

        var responses = await db.Responses
            .AsNoTracking()
            .Where(r => r.SurveyId == id)
            .OrderBy(r => r.SubmittedAt)
            .Select(r => new
            {
                r.SubmittedAt,
                PlayerName = r.Player!.Name,
                Answers = r.Answers.Select(a => new { a.QuestionId, a.ChoiceId, a.Text }).ToList(),
            })
            .ToListAsync();

        var csv = new StringBuilder();
        AppendRow(csv, ["回答日時", "プレイヤー名", .. questions.Select((q, i) => $"Q{i + 1}. {q.Text}")]);
        foreach (var response in responses)
        {
            var cells = new List<string>
            {
                TimeZoneInfo.ConvertTime(response.SubmittedAt, OpsTimeZone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                response.PlayerName,
            };

            foreach (var question in questions)
            {
                var values = response.Answers
                    .Where(a => a.QuestionId == question.Id)
                    .Select(a => a.ChoiceId is { } choiceId ? choiceTexts[choiceId] : a.Text ?? "");
                cells.Add(string.Join(" / ", values));
            }

            AppendRow(csv, cells);
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return ($"survey-{survey.Id}.csv", content);
    }

    public static List<string> Validate(SurveyForm form)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(form.Title) || form.Title.Trim().Length > Survey.TitleMaxLength)
        {
            errors.Add($"タイトルは 1〜{Survey.TitleMaxLength} 文字で入力してください。");
        }

        if (form.Description?.Trim().Length > Survey.DescriptionMaxLength)
        {
            errors.Add($"説明は {Survey.DescriptionMaxLength} 文字以内で入力してください。");
        }

        for (var i = 0; i < form.Questions.Count; i++)
        {
            var question = form.Questions[i];
            var label = $"Q{i + 1}";
            if (string.IsNullOrWhiteSpace(question.Text) || question.Text.Trim().Length > QuestionTextMaxLength)
            {
                errors.Add($"{label}: 質問文は 1〜{QuestionTextMaxLength} 文字で入力してください。");
            }

            if (!question.HasChoices)
            {
                continue;
            }

            if (question.Choices.Count < 2)
            {
                errors.Add($"{label}: 選択肢を 2 つ以上用意してください。");
            }

            if (question.Choices.Any(c => string.IsNullOrWhiteSpace(c.Text) || c.Text.Trim().Length > ChoiceTextMaxLength))
            {
                errors.Add($"{label}: 選択肢は 1〜{ChoiceTextMaxLength} 文字で入力してください。");
            }
        }

        return errors;
    }

    private void Apply(SurveyForm form, Survey survey)
    {
        survey.Title = form.Title.Trim();
        survey.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        survey.ClosesAt = form.ClosesAtLocal is { } local
            ? new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), OpsTimeZone.GetUtcOffset(local)).ToUniversalTime()
            : null;

        survey.Questions = form.Questions.Select((q, order) => new Question
        {
            Order = order,
            Text = q.Text.Trim(),
            Type = q.Type,
            IsRequired = q.IsRequired,
            Choices = q.HasChoices
                ? q.Choices.Select((c, choiceOrder) => new Choice { Order = choiceOrder, Text = c.Text.Trim() }).ToList()
                : [],
        }).ToList();
    }

    private static IQueryable<Survey> LoadWithQuestions(ApplicationDbContext db) =>
        db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Choices);

    private static void AppendRow(StringBuilder csv, IEnumerable<string> cells)
    {
        csv.AppendJoin(',', cells.Select(Escape)).Append("\r\n");
    }

    // カンマ・改行・ダブルクォートを含む値はダブルクォートで囲む。
    // また、Excel で数式として実行されないよう、= + - @ で始まる値には ' を付ける
    private static string Escape(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
