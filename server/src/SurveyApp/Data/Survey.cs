namespace SurveyApp.Data;

public enum SurveyStatus
{
    Draft,
    Published,
    Closed,
}

public class Survey
{
    // ゲームから参照する ID を推測されにくくするため Guid にする
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Title { get; set; }
    public string? Description { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    // null のときは期限なし
    public DateTimeOffset? ClosesAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Question> Questions { get; set; } = [];
    public List<Response> Responses { get; set; } = [];

    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int AnswerTextMaxLength = 4000;

    public bool CanAcceptResponses(DateTimeOffset now) =>
        Status == SurveyStatus.Published && (ClosesAt is null || now < ClosesAt);

    /// <summary>
    /// ゲームから送られた回答を検証し、保存する Answer の一覧を作ります。
    /// 任意の質問は未回答でもよく、選択肢が空・自由記述が空白だけのときは未回答として扱います。
    /// </summary>
    public (List<Answer> Answers, Dictionary<string, string[]> Errors) BuildAnswers(IReadOnlyList<SubmittedAnswer> submitted)
    {
        var errors = new Dictionary<string, string[]>();
        var answers = new List<Answer>();

        var byQuestion = new Dictionary<int, SubmittedAnswer>();
        foreach (var answer in submitted)
        {
            if (!byQuestion.TryAdd(answer.QuestionId, answer))
            {
                errors[$"questions[{answer.QuestionId}]"] = ["同じ質問への回答が重複しています。"];
            }
            else if (Questions.All(q => q.Id != answer.QuestionId))
            {
                errors[$"questions[{answer.QuestionId}]"] = ["このアンケートにない質問です。"];
            }
        }

        foreach (var question in Questions.OrderBy(q => q.Order))
        {
            var key = $"questions[{question.Id}]";
            byQuestion.TryGetValue(question.Id, out var answer);

            if (question.HasChoices)
            {
                var choiceIds = answer?.ChoiceIds?.Distinct().ToList() ?? [];
                if (choiceIds.Count == 0)
                {
                    if (question.IsRequired)
                    {
                        errors[key] = ["必須の質問です。"];
                    }
                }
                else if (question.Type == QuestionType.SingleChoice && choiceIds.Count > 1)
                {
                    errors[key] = ["選択肢は 1 つだけ選んでください。"];
                }
                else if (choiceIds.Any(id => question.Choices.All(c => c.Id != id)))
                {
                    errors[key] = ["この質問にない選択肢です。"];
                }
                else
                {
                    answers.AddRange(choiceIds.Select(id => new Answer { QuestionId = question.Id, ChoiceId = id }));
                }

                continue;
            }

            var text = answer?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                if (question.IsRequired)
                {
                    errors[key] = ["必須の質問です。"];
                }
            }
            else if (text.Length > AnswerTextMaxLength)
            {
                errors[key] = [$"{AnswerTextMaxLength} 文字以内で入力してください。"];
            }
            else
            {
                answers.Add(new Answer { QuestionId = question.Id, Text = text });
            }
        }

        return (answers, errors);
    }
}

// ゲームから送られた 1 問分の回答。選択式は ChoiceIds、自由記述は Text を使う
public record SubmittedAnswer(int QuestionId, IReadOnlyList<int>? ChoiceIds, string? Text);
