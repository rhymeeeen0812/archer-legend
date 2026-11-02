using System.Collections.Generic;

namespace ArcherLegend.Application
{
    public enum SurveyQuestionType
    {
        SingleChoice,
        MultipleChoice,
        FreeText
    }

    /// <summary>
    /// 運営ツールで作られ、サーバーから配信されるゲーム内アンケートです。
    /// </summary>
    public sealed class Survey
    {
        public Survey(string id, string title, string description, IReadOnlyList<SurveyQuestion> questions)
        {
            Id = id;
            Title = title;
            Description = description;
            Questions = questions;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<SurveyQuestion> Questions { get; }
    }

    public sealed class SurveyQuestion
    {
        public SurveyQuestion(int id, string text, SurveyQuestionType type, bool isRequired, IReadOnlyList<SurveyChoice> choices)
        {
            Id = id;
            Text = text;
            Type = type;
            IsRequired = isRequired;
            Choices = choices;
        }

        public int Id { get; }
        public string Text { get; }
        public SurveyQuestionType Type { get; }
        public bool IsRequired { get; }
        public IReadOnlyList<SurveyChoice> Choices { get; }

        public bool HasChoices => Type != SurveyQuestionType.FreeText;
    }

    public readonly struct SurveyChoice
    {
        public SurveyChoice(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public int Id { get; }
        public string Text { get; }
    }

    /// <summary>サーバーへ送る 1 問分の回答です。選択式は ChoiceIds、自由記述は Text を使います。</summary>
    public readonly struct SurveyAnswer
    {
        public SurveyAnswer(int questionId, IReadOnlyList<int> choiceIds, string text)
        {
            QuestionId = questionId;
            ChoiceIds = choiceIds;
            Text = text;
        }

        public int QuestionId { get; }
        public IReadOnlyList<int> ChoiceIds { get; }
        public string Text { get; }
    }
}
