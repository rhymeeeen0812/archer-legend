using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcherLegend.Application
{
    /// <summary>
    /// 回答中のアンケートの入力内容です。どの選択肢を選んだか・次へ進めるかの判断を View から切り離します。
    /// </summary>
    public sealed class SurveyAnswerSheet
    {
        private readonly List<int>[] selectedChoices;
        private readonly string[] texts;

        public SurveyAnswerSheet(Survey survey)
        {
            Survey = survey;
            selectedChoices = survey.Questions.Select(_ => new List<int>()).ToArray();
            texts = new string[survey.Questions.Count];
        }

        public Survey Survey { get; }

        /// <summary>選んだ選択肢の番号（0 始まり）を、選択肢の並び順で返します。</summary>
        public IReadOnlyList<int> GetSelectedChoices(int questionIndex)
        {
            return selectedChoices[questionIndex];
        }

        /// <summary>
        /// 選択肢を押したときの処理です。単一選択は押した選択肢に切り替え、複数選択は選択・解除を切り替えます。
        /// 任意の単一選択は、選択中の選択肢をもう一度押すと未回答に戻せます。
        /// </summary>
        public void ToggleChoice(int questionIndex, int choiceIndex)
        {
            var question = Survey.Questions[questionIndex];
            if (!question.HasChoices || choiceIndex < 0 || choiceIndex >= question.Choices.Count)
            {
                return;
            }

            var selected = selectedChoices[questionIndex];
            if (question.Type == SurveyQuestionType.SingleChoice)
            {
                var clearsAnswer = !question.IsRequired && selected.Contains(choiceIndex);
                selected.Clear();
                if (!clearsAnswer)
                {
                    selected.Add(choiceIndex);
                }

                return;
            }

            if (!selected.Remove(choiceIndex))
            {
                selected.Add(choiceIndex);
                selected.Sort();
            }
        }

        public string GetText(int questionIndex)
        {
            return texts[questionIndex] ?? string.Empty;
        }

        public void SetText(int questionIndex, string text)
        {
            texts[questionIndex] = text;
        }

        public bool IsAnswered(int questionIndex)
        {
            return Survey.Questions[questionIndex].HasChoices
                ? selectedChoices[questionIndex].Count > 0
                : !string.IsNullOrWhiteSpace(texts[questionIndex]);
        }

        /// <summary>必須の質問は回答するまで次へ進めません。</summary>
        public bool CanProceed(int questionIndex)
        {
            return !Survey.Questions[questionIndex].IsRequired || IsAnswered(questionIndex);
        }

        /// <summary>回答した質問だけをサーバーへ送る形にします。</summary>
        public IReadOnlyList<SurveyAnswer> ToAnswers()
        {
            var answers = new List<SurveyAnswer>();
            for (var i = 0; i < Survey.Questions.Count; i++)
            {
                if (!IsAnswered(i))
                {
                    continue;
                }

                var question = Survey.Questions[i];
                answers.Add(question.HasChoices
                    ? new SurveyAnswer(question.Id, selectedChoices[i].Select(index => question.Choices[index].Id).ToArray(), string.Empty)
                    : new SurveyAnswer(question.Id, Array.Empty<int>(), texts[i].Trim()));
            }

            return answers;
        }
    }
}
