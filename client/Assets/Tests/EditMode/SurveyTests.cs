using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Presentation.Surveys;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class SurveyAnswerSheetTests
    {
        [Test]
        public void ToggleChoice_単一選択は押した選択肢に切り替わる()
        {
            var sheet = new SurveyAnswerSheet(TestSurveys.Create());

            sheet.ToggleChoice(0, 0);
            sheet.ToggleChoice(0, 1);

            Assert.That(sheet.GetSelectedChoices(0), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ToggleChoice_複数選択は選択と解除を切り替え並び順で返す()
        {
            var sheet = new SurveyAnswerSheet(TestSurveys.Create());

            sheet.ToggleChoice(1, 2);
            sheet.ToggleChoice(1, 0);
            sheet.ToggleChoice(1, 1);
            sheet.ToggleChoice(1, 1);

            Assert.That(sheet.GetSelectedChoices(1), Is.EqualTo(new[] { 0, 2 }));
        }

        [Test]
        public void ToggleChoice_必須の単一選択はもう一度押しても選択されたまま()
        {
            var sheet = new SurveyAnswerSheet(TestSurveys.Create());

            sheet.ToggleChoice(0, 1);
            sheet.ToggleChoice(0, 1);

            Assert.That(sheet.GetSelectedChoices(0), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ToggleChoice_任意の単一選択はもう一度押すと未回答に戻る()
        {
            var survey = new Survey("survey-1", "満足度調査", string.Empty, new[]
            {
                new SurveyQuestion(1, "楽しかったですか？", SurveyQuestionType.SingleChoice, false,
                    new[] { new SurveyChoice(11, "とても楽しい"), new SurveyChoice(12, "楽しい") })
            });
            var sheet = new SurveyAnswerSheet(survey);

            sheet.ToggleChoice(0, 1);
            sheet.ToggleChoice(0, 1);

            Assert.That(sheet.GetSelectedChoices(0), Is.Empty);
            Assert.That(sheet.ToAnswers(), Is.Empty);
        }

        [Test]
        public void CanProceed_必須の質問は回答するまで進めず任意の質問は空でも進める()
        {
            var sheet = new SurveyAnswerSheet(TestSurveys.Create());

            Assert.That(sheet.CanProceed(0), Is.False);
            Assert.That(sheet.CanProceed(1), Is.True);

            sheet.ToggleChoice(0, 0);
            Assert.That(sheet.CanProceed(0), Is.True);
        }

        [Test]
        public void ToAnswers_回答した質問だけを選択肢のIdと前後の空白を除いた文章で返す()
        {
            var sheet = new SurveyAnswerSheet(TestSurveys.Create());
            sheet.ToggleChoice(0, 1);
            sheet.SetText(2, "  もっと遊びたい  ");

            var answers = sheet.ToAnswers();

            Assert.That(answers.Select(a => a.QuestionId), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(answers[0].ChoiceIds, Is.EqualTo(new[] { 12 }));
            Assert.That(answers[1].Text, Is.EqualTo("もっと遊びたい"));
        }
    }

    public class SurveyServiceTests
    {
        [Test]
        public async Task GetAvailableAsync_サーバーからプレイヤーが消えていたら登録し直して取得する()
        {
            var api = new FakeGameApiClient { SurveyNotFoundCount = 1 };
            api.Surveys.Add(TestSurveys.Create());
            var repository = new InMemorySaveDataRepository
            {
                Stored = new SaveData { playerId = "player-1", playerName = "アーチャー", isRegistered = true }
            };

            var surveys = await CreateService(repository, api).GetAvailableAsync();

            Assert.That(surveys.Count, Is.EqualTo(1));
            Assert.That(api.RegisteredIds, Is.EqualTo(new[] { "player-1" }));
        }

        [Test]
        public async Task GetAvailableAsync_プレイ結果の送信に失敗していてもアンケートを取得でき送信は試みない()
        {
            var api = new FakeGameApiClient { SubmitFailure = _ => new GameApiException(400, "bad request") };
            api.Surveys.Add(TestSurveys.Create());
            var repository = new InMemorySaveDataRepository
            {
                Stored = new SaveData
                {
                    playerId = "player-1",
                    playerName = "アーチャー",
                    isRegistered = true,
                    pendingRuns = new List<PendingRun> { new PendingRun { runId = "run-1" } }
                }
            };

            var surveys = await CreateService(repository, api).GetAvailableAsync();

            Assert.That(surveys.Count, Is.EqualTo(1));
            Assert.That(api.SubmittedRunIds, Is.Empty);
        }

        private static SurveyService CreateService(InMemorySaveDataRepository repository, FakeGameApiClient api)
        {
            var progress = new PlayerProgressService(repository);
            return new SurveyService(progress, new OnlineSyncService(progress, api), api);
        }
    }

    public class SurveyPresenterTests
    {
        [Test]
        public void Open_最初の質問を表示し必須なら選ぶまで次へ進めない()
        {
            var (view, presenter, _) = Create();

            presenter.Open(TestSurveys.Create());

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.PageIndex, Is.Zero);
            Assert.That(view.Choices, Is.EqualTo(new[] { "とても楽しい", "楽しい" }));
            Assert.That(view.CanGoBack, Is.False);
            Assert.That(view.CanProceed, Is.False);

            view.ClickChoice(1);
            Assert.That(view.Selected, Is.EqualTo(new[] { 1 }));
            Assert.That(view.CanProceed, Is.True);
        }

        [Test]
        public void 戻ると選んだ内容と入力した文章が残っている()
        {
            var (view, presenter, _) = Create();
            presenter.Open(TestSurveys.Create());
            view.ClickChoice(0);
            view.ClickNext();
            view.ClickNext();
            view.InputText("楽しかった");

            view.ClickBack();
            view.ClickBack();
            Assert.That(view.Selected, Is.EqualTo(new[] { 0 }));

            view.ClickNext();
            view.ClickNext();
            Assert.That(view.Text, Is.EqualTo("楽しかった"));
            Assert.That(view.Choices, Is.Null);
            Assert.That(view.IsLastPage, Is.True);
        }

        [Test]
        public async Task 最後のページで送信すると回答を送ってお礼を表示し閉じると回答済みとして通知する()
        {
            var (view, presenter, api) = Create();
            api.PendingSurveySubmit = new TaskCompletionSource<bool>();
            bool? closedAnswered = null;
            presenter.Closed += answered => closedAnswered = answered;
            presenter.Open(TestSurveys.Create());
            view.ClickChoice(0);
            view.ClickNext();
            view.ClickNext();

            view.ClickNext();
            view.ClickClose();
            Assert.That(view.IsVisible, Is.True, "送信中は閉じない");

            api.PendingSurveySubmit.SetResult(true);
            for (var i = 0; i < 10 && view.Message == null; i++)
            {
                await Task.Yield();
            }

            Assert.That(api.SubmittedSurveys.Single().surveyId, Is.EqualTo("survey-1"));
            Assert.That(view.Message, Does.Contain("ありがとうございました"));

            view.ClickClose();
            Assert.That(view.IsVisible, Is.False);
            Assert.That(closedAnswered, Is.True);
        }

        [Test]
        public void 送信に失敗したらエラーを表示してもう一度送れる()
        {
            var (view, presenter, api) = Create();
            api.SubmitSurveyException = new GameApiException(0, "offline");
            presenter.Open(TestSurveys.Create());
            view.ClickChoice(0);
            view.ClickNext();
            view.ClickNext();

            view.ClickNext();

            Assert.That(view.Error, Does.Contain("送信できませんでした"));
            Assert.That(view.CanProceed, Is.True);

            api.SubmitSurveyException = null;
            view.ClickNext();
            Assert.That(api.SubmittedSurveys, Has.Count.EqualTo(1));
        }

        [Test]
        public void 回答済みや受付終了なら送り直さずに終了扱いにする()
        {
            var (view, presenter, api) = Create();
            api.SubmitSurveyException = new GameApiException(409, "conflict");
            bool? closedAnswered = null;
            presenter.Closed += answered => closedAnswered = answered;
            presenter.Open(TestSurveys.Create());
            view.ClickChoice(0);
            view.ClickNext();
            view.ClickNext();

            view.ClickNext();
            view.ClickClose();

            Assert.That(view.Message, Does.Contain("受付を終了"));
            Assert.That(closedAnswered, Is.True);
        }

        [TestCase(400)]
        [TestCase(404)]
        public void 送り直しても成功しないエラーは通信エラーとして案内せず終了扱いにする(int statusCode)
        {
            var (view, presenter, api) = Create();
            api.SubmitSurveyException = new GameApiException(statusCode, "rejected");
            bool? closedAnswered = null;
            presenter.Closed += answered => closedAnswered = answered;
            presenter.Open(TestSurveys.Create());
            view.ClickChoice(0);
            view.ClickNext();
            view.ClickNext();

            view.ClickNext();
            view.ClickClose();

            Assert.That(view.Message, Does.Contain("回答できませんでした"));
            Assert.That(closedAnswered, Is.True);
        }

        [Test]
        public void 説明文があれば質問の前に表示し戻ることもできる()
        {
            var (view, presenter, _) = Create();

            presenter.Open(TestSurveys.Create("ご協力をお願いします"));
            Assert.That(view.Intro, Is.EqualTo("ご協力をお願いします"));

            view.ClickNext();
            Assert.That(view.Intro, Is.Null);
            Assert.That(view.PageIndex, Is.Zero);
            Assert.That(view.CanGoBack, Is.True);

            view.ClickBack();
            Assert.That(view.Intro, Is.EqualTo("ご協力をお願いします"));
        }

        [Test]
        public void 途中で閉じたら未回答として通知する()
        {
            var (view, presenter, _) = Create();
            bool? closedAnswered = null;
            presenter.Closed += answered => closedAnswered = answered;
            presenter.Open(TestSurveys.Create());

            view.ClickClose();

            Assert.That(view.IsVisible, Is.False);
            Assert.That(closedAnswered, Is.False);
        }

        private static (FakeSurveyView view, SurveyPresenter presenter, FakeGameApiClient api) Create()
        {
            var api = new FakeGameApiClient();
            var progress = new PlayerProgressService(new InMemorySaveDataRepository());
            var service = new SurveyService(progress, new OnlineSyncService(progress, api), api);
            var view = new FakeSurveyView();
            return (view, new SurveyPresenter(view, service), api);
        }
    }
}
