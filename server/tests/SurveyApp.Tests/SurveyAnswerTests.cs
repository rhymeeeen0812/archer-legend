using SurveyApp.Data;

namespace SurveyApp.Tests;

public class SurveyAnswerTests
{
    // Q1: 単一選択（必須） / Q2: 複数選択（任意） / Q3: 自由記述（任意）
    private static Survey CreateSurvey() => new()
    {
        Title = "テスト",
        Questions =
        [
            new Question
            {
                Id = 1, Order = 0, Text = "単一", Type = QuestionType.SingleChoice, IsRequired = true,
                Choices = [new Choice { Id = 11, Text = "A" }, new Choice { Id = 12, Text = "B" }],
            },
            new Question
            {
                Id = 2, Order = 1, Text = "複数", Type = QuestionType.MultipleChoice,
                Choices = [new Choice { Id = 21, Text = "C" }, new Choice { Id = 22, Text = "D" }],
            },
            new Question { Id = 3, Order = 2, Text = "自由", Type = QuestionType.FreeText },
        ],
    };

    [Fact]
    public void BuildAnswers_選択肢は1つにつき1行で自由記述は前後の空白を除いて保存する()
    {
        var (answers, errors) = CreateSurvey().BuildAnswers(
        [
            new SubmittedAnswer(1, [12], null),
            new SubmittedAnswer(2, [21, 22, 21], null),
            new SubmittedAnswer(3, null, "  楽しかった  "),
        ]);

        Assert.Empty(errors);
        Assert.Equal([12, 21, 22], answers.Where(a => a.ChoiceId != null).Select(a => a.ChoiceId!.Value));
        Assert.Equal("楽しかった", Assert.Single(answers, a => a.QuestionId == 3).Text);
    }

    [Fact]
    public void BuildAnswers_任意の質問は空でもよい()
    {
        var (answers, errors) = CreateSurvey().BuildAnswers(
        [
            new SubmittedAnswer(1, [11], null),
            new SubmittedAnswer(2, [], null),
            new SubmittedAnswer(3, null, "   "),
        ]);

        Assert.Empty(errors);
        Assert.Single(answers);
    }

    [Fact]
    public void BuildAnswers_必須の質問が未回答ならエラー()
    {
        var (_, errors) = CreateSurvey().BuildAnswers([]);

        Assert.Contains("questions[1]", errors.Keys);
    }

    [Fact]
    public void BuildAnswers_単一選択で複数選んだらエラー()
    {
        var (_, errors) = CreateSurvey().BuildAnswers([new SubmittedAnswer(1, [11, 12], null)]);

        Assert.Contains("questions[1]", errors.Keys);
    }

    [Fact]
    public void BuildAnswers_ほかの質問の選択肢やアンケートにない質問はエラー()
    {
        var (_, errors) = CreateSurvey().BuildAnswers(
        [
            new SubmittedAnswer(1, [21], null),
            new SubmittedAnswer(99, [], "x"),
        ]);

        Assert.Contains("questions[1]", errors.Keys);
        Assert.Contains("questions[99]", errors.Keys);
    }

    [Fact]
    public void BuildAnswers_自由記述が長すぎたらエラー()
    {
        var (_, errors) = CreateSurvey().BuildAnswers(
        [
            new SubmittedAnswer(1, [11], null),
            new SubmittedAnswer(3, null, new string('あ', Survey.AnswerTextMaxLength + 1)),
        ]);

        Assert.Contains("questions[3]", errors.Keys);
    }
}
