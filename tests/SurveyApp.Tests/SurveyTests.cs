using SurveyApp.Data;

namespace SurveyApp.Tests;

public class SurveyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static Survey CreateSurvey(SurveyStatus status, DateTimeOffset? closesAt = null) =>
        new() { OwnerId = "owner", Title = "テスト", Status = status, ClosesAt = closesAt };

    [Theory]
    [InlineData(SurveyStatus.Draft, false)]
    [InlineData(SurveyStatus.Published, true)]
    [InlineData(SurveyStatus.Closed, false)]
    public void CanAcceptResponses_期限なしのときは公開中だけ受け付ける(SurveyStatus status, bool expected)
    {
        Assert.Equal(expected, CreateSurvey(status).CanAcceptResponses(Now));
    }

    [Fact]
    public void CanAcceptResponses_期限前なら受け付ける()
    {
        Assert.True(CreateSurvey(SurveyStatus.Published, Now.AddMinutes(1)).CanAcceptResponses(Now));
    }

    [Fact]
    public void CanAcceptResponses_期限ちょうど以降は受け付けない()
    {
        Assert.False(CreateSurvey(SurveyStatus.Published, Now).CanAcceptResponses(Now));
        Assert.False(CreateSurvey(SurveyStatus.Published, Now.AddMinutes(-1)).CanAcceptResponses(Now));
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice, true)]
    [InlineData(QuestionType.MultipleChoice, true)]
    [InlineData(QuestionType.FreeText, false)]
    public void HasChoices_選択式の質問だけtrue(QuestionType type, bool expected)
    {
        Assert.Equal(expected, new Question { Text = "Q", Type = type }.HasChoices);
    }
}
