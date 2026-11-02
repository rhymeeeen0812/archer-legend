using SurveyApp.Data;

namespace SurveyApp.Admin;

// 運営ツールの編集フォームの入力値。Blazor のフォームにそのまま結び付けるので、変更可能なクラスにしている
public class SurveyForm
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    // 運営ツールのタイムゾーン（日本時間）での締め切り。null なら期限なし
    public DateTime? ClosesAtLocal { get; set; }

    public List<QuestionForm> Questions { get; set; } = [];
}

public class QuestionForm
{
    public string Text { get; set; } = "";
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public bool IsRequired { get; set; } = true;
    public List<ChoiceForm> Choices { get; set; } = [new(), new()];

    public bool HasChoices => Type is QuestionType.SingleChoice or QuestionType.MultipleChoice;
}

public class ChoiceForm
{
    public string Text { get; set; } = "";
}

public record SurveySummary(
    Guid Id,
    string Title,
    SurveyStatus Status,
    DateTimeOffset? ClosesAt,
    int QuestionCount,
    int ResponseCount,
    DateTimeOffset UpdatedAt);

public record SurveyResults(
    Guid Id,
    string Title,
    string? Description,
    SurveyStatus Status,
    DateTimeOffset? ClosesAt,
    int ResponseCount,
    IReadOnlyList<QuestionResult> Questions);

// AnsweredCount はこの質問に回答した人数（任意の質問は全体の回答数より少なくなる）
public record QuestionResult(
    int Order,
    string Text,
    QuestionType Type,
    bool IsRequired,
    int AnsweredCount,
    IReadOnlyList<ChoiceResult> Choices,
    IReadOnlyList<string> TextAnswers);

public record ChoiceResult(string Text, int Count);
