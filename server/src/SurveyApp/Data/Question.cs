namespace SurveyApp.Data;

public enum QuestionType
{
    SingleChoice,
    MultipleChoice,
    FreeText,
}

public class Question
{
    public int Id { get; set; }

    public Guid SurveyId { get; set; }
    public Survey? Survey { get; set; }

    // アンケート内での表示順（0 始まり）
    public int Order { get; set; }

    public required string Text { get; set; }
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }

    public List<Choice> Choices { get; set; } = [];

    public bool HasChoices => Type is QuestionType.SingleChoice or QuestionType.MultipleChoice;
}

public class Choice
{
    public int Id { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    // 質問内での表示順（0 始まり）
    public int Order { get; set; }

    public required string Text { get; set; }
}
