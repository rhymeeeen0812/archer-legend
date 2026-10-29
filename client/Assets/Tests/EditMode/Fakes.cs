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

        public void SetStatus(int health, int maxHealth, int level, int experience, int experienceToNext, int wave, int score, int aliveEnemies)
        {
            Health = health;
        }

        public void ShowUpgrades(IReadOnlyList<UpgradeOption> options)
        {
            ShowUpgradesCount++;
            IsUpgradesVisible = true;
        }

        public void HideUpgrades() => IsUpgradesVisible = false;

        public void SelectUpgrade(int index) => UpgradeSelected?.Invoke(index);
    }
}
