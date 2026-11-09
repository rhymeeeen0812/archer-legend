using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using ArcherLegend.Presentation;
using ArcherLegend.Presentation.Game;
using ArcherLegend.Presentation.Home;
using ArcherLegend.Presentation.Result;
using ArcherLegend.Presentation.Surveys;

namespace ArcherLegend.Tests
{
    internal sealed class InMemorySaveDataRepository : ISaveDataRepository
    {
        public SaveData Stored { get; set; }
        public Exception LoadException { get; set; }
        public Exception SaveException { get; set; }
        public int SaveCount { get; private set; }

        /// <summary>設定すると、読み込みはこの Task が完了するまで終わらない（通信待ちの再現用）。</summary>
        public TaskCompletionSource<SaveData> PendingLoad { get; set; }

        public Task<SaveData> LoadAsync(CancellationToken cancellationToken)
        {
            if (PendingLoad != null)
            {
                return PendingLoad.Task;
            }

            if (LoadException != null)
            {
                return Task.FromException<SaveData>(LoadException);
            }

            return Task.FromResult(Stored);
        }

        public Task SaveAsync(SaveData data, CancellationToken cancellationToken)
        {
            if (SaveException != null)
            {
                return Task.FromException(SaveException);
            }

            Stored = data;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeGameApiClient : IGameApiClient
    {
        public List<string> RegisteredIds { get; } = new List<string>();
        public List<string> SubmittedRunIds { get; } = new List<string>();
        public Exception RegisterException { get; set; }
        public Exception RankingException { get; set; }
        public Ranking Ranking { get; set; } = new Ranking(Array.Empty<RankingEntry>(), null);

        /// <summary>送信するプレイ結果ごとに、投げる例外を決めます（null なら成功）。</summary>
        public Func<PendingRun, Exception> SubmitFailure { get; set; }

        /// <summary>設定すると、送信はこの Task が完了するまで終わらない（通信待ちの再現用）。</summary>
        public TaskCompletionSource<bool> PendingSubmit { get; set; }

        public Task RegisterPlayerAsync(string playerId, string name, CancellationToken cancellationToken)
        {
            if (RegisterException != null)
            {
                return Task.FromException(RegisterException);
            }

            RegisteredIds.Add(playerId);
            return Task.CompletedTask;
        }

        public async Task SubmitRunAsync(string playerId, PendingRun run, CancellationToken cancellationToken)
        {
            if (PendingSubmit != null)
            {
                await PendingSubmit.Task;
            }

            var failure = SubmitFailure?.Invoke(run);
            if (failure != null)
            {
                throw failure;
            }

            SubmittedRunIds.Add(run.runId);
        }

        public Task<Ranking> GetRankingAsync(string playerId, int limit, CancellationToken cancellationToken)
        {
            return RankingException != null ? Task.FromException<Ranking>(RankingException) : Task.FromResult(Ranking);
        }

        public List<Survey> Surveys { get; } = new List<Survey>();
        public Exception SurveyException { get; set; }
        public Exception SubmitSurveyException { get; set; }
        public List<(string surveyId, IReadOnlyList<SurveyAnswer> answers)> SubmittedSurveys { get; } = new List<(string, IReadOnlyList<SurveyAnswer>)>();

        /// <summary>設定すると、アンケートの送信はこの Task が完了するまで終わらない（通信待ちの再現用）。</summary>
        public TaskCompletionSource<bool> PendingSurveySubmit { get; set; }

        /// <summary>この回数だけ、アンケートの取得に 404 を返します（サーバーからプレイヤーが消えた状態の再現用）。</summary>
        public int SurveyNotFoundCount { get; set; }

        public Task<IReadOnlyList<Survey>> GetSurveysAsync(string playerId, CancellationToken cancellationToken)
        {
            if (SurveyNotFoundCount > 0)
            {
                SurveyNotFoundCount--;
                return Task.FromException<IReadOnlyList<Survey>>(new GameApiException(404, "not found"));
            }

            return SurveyException != null
                ? Task.FromException<IReadOnlyList<Survey>>(SurveyException)
                : Task.FromResult<IReadOnlyList<Survey>>(Surveys.ToArray());
        }

        public async Task SubmitSurveyResponseAsync(string playerId, string surveyId, IReadOnlyList<SurveyAnswer> answers, CancellationToken cancellationToken)
        {
            if (PendingSurveySubmit != null)
            {
                await PendingSurveySubmit.Task;
            }

            if (SubmitSurveyException != null)
            {
                throw SubmitSurveyException;
            }

            SubmittedSurveys.Add((surveyId, answers));
        }
    }

    internal sealed class FakeSceneNavigator : ISceneNavigator
    {
        public string Last { get; private set; }

        public void GoToTitle() => Last = "Title";
        public void GoToHome() => Last = "Home";
        public void GoToGame() => Last = "Game";
    }

    internal sealed class FakeHomeView : IHomeView
    {
        public event Action PlayClicked;
        public event Action BackToTitleClicked;

        public string State { get; private set; }
        public (int bestScore, int bestWave, int totalEnemiesDefeated, int playCount) Records { get; private set; }

        public void ShowLoading() => State = "Loading";

        public void ShowRecords(int bestScore, int bestWave, int totalEnemiesDefeated, int playCount)
        {
            State = "Records";
            Records = (bestScore, bestWave, totalEnemiesDefeated, playCount);
        }

        public void ShowLoadError() => State = "LoadError";

        public string RankingState { get; private set; }
        public Ranking Ranking { get; private set; }

        public void ShowRankingLoading() => RankingState = "Loading";

        public void ShowRanking(Ranking ranking)
        {
            RankingState = "Ranking";
            Ranking = ranking;
        }

        public void ShowRankingUnavailable() => RankingState = "Unavailable";

        public event Action SurveyClicked;
        public bool IsSurveyAvailable { get; private set; }

        public void SetSurveyAvailable(bool available) => IsSurveyAvailable = available;
        public void ClickSurvey() => SurveyClicked?.Invoke();

        public void ClickPlay() => PlayClicked?.Invoke();
        public void ClickBackToTitle() => BackToTitleClicked?.Invoke();
    }

    internal sealed class FakeResultView : IResultView
    {
        public event Action RetryClicked;
        public event Action HomeClicked;

        public bool IsVisible { get; private set; }
        public bool IsSaveErrorShown { get; private set; }
        public int Score { get; private set; }
        public bool IsNewBestScore { get; private set; }
        public bool IsNewBestWave { get; private set; }

        public void Show(int score, int wave, int enemiesDefeated, int level, bool isNewBestScore, bool isNewBestWave)
        {
            IsVisible = true;
            Score = score;
            IsNewBestScore = isNewBestScore;
            IsNewBestWave = isNewBestWave;
        }

        public void ShowSaveError() => IsSaveErrorShown = true;
        public void Hide() => IsVisible = false;

        public void ClickRetry() => RetryClicked?.Invoke();
        public void ClickHome() => HomeClicked?.Invoke();
    }

    internal sealed class FakeGameHudView : IGameHudView
    {
        public event Action<int> UpgradeSelected;

        public int ShowUpgradesCount { get; private set; }
        public bool IsUpgradesVisible { get; private set; }
        public int Health { get; private set; }
        public int Stage { get; private set; }
        public int WaveInStage { get; private set; }
        public bool IsExitOpen { get; private set; }

        public void SetStatus(int health, int maxHealth, int level, int experience, int experienceToNext, int stage, int waveInStage, int score, int aliveEnemies, bool isExitOpen)
        {
            Health = health;
            Stage = stage;
            WaveInStage = waveInStage;
            IsExitOpen = isExitOpen;
        }

        public void ShowUpgrades(IReadOnlyList<UpgradeOption> options)
        {
            ShowUpgradesCount++;
            IsUpgradesVisible = true;
        }

        public void HideUpgrades() => IsUpgradesVisible = false;

        public void SelectUpgrade(int index) => UpgradeSelected?.Invoke(index);
    }

    internal sealed class FakeSurveyView : ISurveyView
    {
        public event Action<int> ChoiceClicked;
        public event Action<string> TextChanged;
        public event Action NextClicked;
        public event Action BackClicked;
        public event Action CloseClicked;

        public bool IsVisible { get; private set; }
        public int PageIndex { get; private set; }
        public IReadOnlyList<string> Choices { get; private set; }
        public string Text { get; private set; }
        public IReadOnlyCollection<int> Selected { get; private set; }
        public bool CanGoBack { get; private set; }
        public bool CanProceed { get; private set; }
        public bool IsLastPage { get; private set; }
        public string Error { get; private set; }
        public string Message { get; private set; }

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;

        public string Intro { get; private set; }

        public void ShowIntro(string surveyTitle, string description) => Intro = description;

        public void ShowQuestion(string surveyTitle, int pageIndex, int pageCount, string question, string hint, IReadOnlyList<string> choices, string text)
        {
            Intro = null;
            PageIndex = pageIndex;
            Choices = choices;
            Text = text;
            Error = null;
            Message = null;
        }

        public void SetSelectedChoices(IReadOnlyCollection<int> selected) => Selected = selected;

        public void SetNavigation(bool canGoBack, bool canProceed, bool isLastPage)
        {
            CanGoBack = canGoBack;
            CanProceed = canProceed;
            IsLastPage = isLastPage;
        }

        public void ShowSubmitting() => Error = "Submitting";
        public void ShowError(string message) => Error = message;
        public void ShowMessage(string message) => Message = message;

        public void ClickChoice(int index) => ChoiceClicked?.Invoke(index);
        public void InputText(string text) => TextChanged?.Invoke(text);
        public void ClickNext() => NextClicked?.Invoke();
        public void ClickBack() => BackClicked?.Invoke();
        public void ClickClose() => CloseClicked?.Invoke();
    }

    internal static class TestSurveys
    {
        /// <summary>Q1: 単一選択（必須） / Q2: 複数選択（任意） / Q3: 自由記述（任意）</summary>
        public static Survey Create(string description = "")
        {
            return new Survey("survey-1", "満足度調査", description, new[]
            {
                new SurveyQuestion(1, "楽しかったですか？", SurveyQuestionType.SingleChoice, true,
                    new[] { new SurveyChoice(11, "とても楽しい"), new SurveyChoice(12, "楽しい") }),
                new SurveyQuestion(2, "好きな強化は？", SurveyQuestionType.MultipleChoice, false,
                    new[] { new SurveyChoice(21, "連射"), new SurveyChoice(22, "強弓"), new SurveyChoice(23, "活力") }),
                new SurveyQuestion(3, "ご意見をどうぞ", SurveyQuestionType.FreeText, false, Array.Empty<SurveyChoice>())
            });
        }
    }
}
