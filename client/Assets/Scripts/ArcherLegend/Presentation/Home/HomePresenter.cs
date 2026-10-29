using System;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using UnityEngine;

namespace ArcherLegend.Presentation.Home
{
    public sealed class HomePresenter : IDisposable
    {
        private const int RankingLimit = 5;

        private readonly IHomeView view;
        private readonly PlayerProgressService progress;
        private readonly OnlineSyncService sync;
        private readonly ISceneNavigator navigator;
        private bool isDisposed;

        public HomePresenter(IHomeView view, PlayerProgressService progress, OnlineSyncService sync, ISceneNavigator navigator)
        {
            this.view = view;
            this.progress = progress;
            this.sync = sync;
            this.navigator = navigator;
            view.PlayClicked += OnPlayClicked;
            view.BackToTitleClicked += OnBackToTitleClicked;
        }

        public async Task InitializeAsync()
        {
            view.ShowLoading();

            UserData userData;
            try
            {
                userData = await progress.LoadAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (!isDisposed)
                {
                    view.ShowLoadError();
                }

                return;
            }

            // 読み込み中に画面を離れていたら、破棄済みの View に触らない
            if (isDisposed)
            {
                return;
            }

            view.ShowRecords(userData.BestScore, userData.BestWave, userData.TotalEnemiesDefeated, userData.PlayCount);
            await ShowRankingAsync();
        }

        // 戦績はローカルのデータですぐに表示し、ランキングは通信が終わってから表示する
        private async Task ShowRankingAsync()
        {
            view.ShowRankingLoading();

            Ranking ranking;
            try
            {
                ranking = await sync.GetRankingAsync(RankingLimit);
            }
            catch (Exception e)
            {
                // オフラインでも遊べるので、エラーではなく警告として残す
                Debug.LogWarning($"Failed to load ranking: {e.Message}");
                if (!isDisposed)
                {
                    view.ShowRankingUnavailable();
                }

                return;
            }

            if (!isDisposed)
            {
                view.ShowRanking(ranking);
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            view.PlayClicked -= OnPlayClicked;
            view.BackToTitleClicked -= OnBackToTitleClicked;
        }

        private void OnPlayClicked()
        {
            navigator.GoToGame();
        }

        private void OnBackToTitleClicked()
        {
            navigator.GoToTitle();
        }
    }
}
