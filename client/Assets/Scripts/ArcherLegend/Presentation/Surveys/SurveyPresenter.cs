using System;
using System.Linq;
using System.Threading.Tasks;
using ArcherLegend.Application;
using UnityEngine;

namespace ArcherLegend.Presentation.Surveys
{
    /// <summary>
    /// ゲーム内アンケートの回答画面です。1 ページに 1 問ずつ表示し、最後のページで送信します。
    /// </summary>
    public sealed class SurveyPresenter : IDisposable
    {
        private readonly ISurveyView view;
        private readonly SurveyService service;
        private SurveyAnswerSheet sheet;
        private int page;
        private bool isSubmitting;
        private bool isFinished;
        private bool isDisposed;
        private bool isShowingIntro;

        public SurveyPresenter(ISurveyView view, SurveyService service)
        {
            this.view = view;
            this.service = service;
            view.ChoiceClicked += OnChoiceClicked;
            view.TextChanged += OnTextChanged;
            view.NextClicked += OnNextClicked;
            view.BackClicked += OnBackClicked;
            view.CloseClicked += OnCloseClicked;
        }

        /// <summary>画面を閉じたときに通知します。引数は回答が終わったか（回答済み・受付終了を含む）です。</summary>
        public event Action<bool> Closed;

        public void Open(Survey survey)
        {
            sheet = new SurveyAnswerSheet(survey);
            page = 0;
            isSubmitting = false;
            isFinished = false;
            view.Show();
            if (HasIntro())
            {
                ShowIntro();
            }
            else
            {
                isShowingIntro = false;
                ShowPage();
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            view.ChoiceClicked -= OnChoiceClicked;
            view.TextChanged -= OnTextChanged;
            view.NextClicked -= OnNextClicked;
            view.BackClicked -= OnBackClicked;
            view.CloseClicked -= OnCloseClicked;
        }

        private bool HasIntro()
        {
            return !string.IsNullOrWhiteSpace(sheet.Survey.Description);
        }

        // 説明文があるときは、最初の質問の前に 1 ページ表示する
        private void ShowIntro()
        {
            isShowingIntro = true;
            view.ShowIntro(sheet.Survey.Title, sheet.Survey.Description);
        }

        private void ShowPage()
        {
            var question = sheet.Survey.Questions[page];
            var choices = question.HasChoices ? question.Choices.Select(choice => choice.Text).ToArray() : null;
            view.ShowQuestion(sheet.Survey.Title, page, sheet.Survey.Questions.Count, question.Text, Hint(question), choices, sheet.GetText(page));
            view.SetSelectedChoices(sheet.GetSelectedChoices(page));
            UpdateNavigation();
        }

        private void UpdateNavigation()
        {
            var isLastPage = page == sheet.Survey.Questions.Count - 1;
            view.SetNavigation(canGoBack: page > 0 || HasIntro(), canProceed: sheet.CanProceed(page), isLastPage: isLastPage);
        }

        private static string Hint(SurveyQuestion question)
        {
            var hint = question.Type switch
            {
                SurveyQuestionType.SingleChoice => "1 つ選んでください",
                SurveyQuestionType.MultipleChoice => "当てはまるものをすべて選んでください",
                _ => "自由に入力してください"
            };
            return question.IsRequired ? $"{hint}（必須）" : $"{hint}（任意）";
        }

        private void OnChoiceClicked(int choiceIndex)
        {
            if (!CanEdit() || isShowingIntro)
            {
                return;
            }

            sheet.ToggleChoice(page, choiceIndex);
            view.SetSelectedChoices(sheet.GetSelectedChoices(page));
            UpdateNavigation();
        }

        private void OnTextChanged(string text)
        {
            if (!CanEdit() || isShowingIntro)
            {
                return;
            }

            sheet.SetText(page, text);
            UpdateNavigation();
        }

        private void OnBackClicked()
        {
            if (!CanEdit() || isShowingIntro)
            {
                return;
            }

            if (page == 0)
            {
                if (HasIntro())
                {
                    ShowIntro();
                }

                return;
            }

            page--;
            ShowPage();
        }

        private void OnNextClicked()
        {
            if (!CanEdit())
            {
                return;
            }

            if (isShowingIntro)
            {
                isShowingIntro = false;
                ShowPage();
                return;
            }

            if (!sheet.CanProceed(page))
            {
                return;
            }

            if (page < sheet.Survey.Questions.Count - 1)
            {
                page++;
                ShowPage();
                return;
            }

            _ = SubmitAsync();
        }

        private void OnCloseClicked()
        {
            // 送信中に閉じると結果を表示できないので、送信が終わるまで待ってもらう
            if (isSubmitting || sheet == null)
            {
                return;
            }

            view.Hide();
            var answered = isFinished;
            sheet = null;
            Closed?.Invoke(answered);
        }

        private bool CanEdit()
        {
            return sheet != null && !isSubmitting && !isFinished;
        }

        private async Task SubmitAsync()
        {
            isSubmitting = true;
            view.ShowSubmitting();
            try
            {
                await service.SubmitAsync(sheet);
                isFinished = true;
                if (!isDisposed)
                {
                    view.ShowMessage("ご回答ありがとうございました！");
                }
            }
            catch (GameApiException e) when (e.IsConflict)
            {
                // ほかの端末などで回答済み、または受付が終わっていた。送り直しても成功しないので終了扱いにする
                isFinished = true;
                if (!isDisposed)
                {
                    view.ShowMessage("このアンケートは回答済みか、受付を終了しています。");
                }
            }
            catch (GameApiException e) when (!e.IsRetryable)
            {
                // 内容の不備やアンケートの削除など。送り直しても成功しないので、ホームに戻って最新の状態を取り直してもらう
                Debug.LogWarning($"Survey submission was rejected: {e.Message}");
                isFinished = true;
                if (!isDisposed)
                {
                    view.ShowMessage("このアンケートには回答できませんでした。お手数ですが、時間をおいて開き直してください。");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to submit survey: {e.Message}");
                if (!isDisposed)
                {
                    view.ShowError("送信できませんでした。通信環境を確認して、もう一度お試しください。");
                    UpdateNavigation();
                }
            }
            finally
            {
                isSubmitting = false;
            }
        }
    }
}
