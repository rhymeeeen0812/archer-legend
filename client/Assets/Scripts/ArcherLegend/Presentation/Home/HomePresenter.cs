using System;
using System.Collections.Generic;
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
        private readonly SurveyService surveys;
        private readonly ISceneNavigator navigator;
        private IReadOnlyList<Survey> availableSurveys = Array.Empty<Survey>();
        private bool isDisposed;

        public HomePresenter(IHomeView view, PlayerProgressService progress, OnlineSyncService sync, SurveyService surveys, ISceneNavigator navigator)
        {
            this.view = view;
            this.progress = progress;
            this.sync = sync;
            this.surveys = surveys;
            this.navigator = navigator;
            view.PlayClicked += OnPlayClicked;
            view.BackToTitleClicked += OnBackToTitleClicked;
            view.SurveyClicked += OnSurveyClicked;
        }

        /// <summary>「アンケートに答える」が押されたときに、回答してもらうアンケートを通知します。</summary>
        public event Action<Survey> SurveyRequested;

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
            await RefreshSurveysAsync();
        }

        /// <summary>回答できるアンケートがあるかを確かめ直します（アンケートに回答した後にも呼びます）。</summary>
        public async Task RefreshSurveysAsync()
        {
            try
            {
                availableSurveys = await surveys.GetAvailableAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load surveys: {e.Message}");
                availableSurveys = Array.Empty<Survey>();
            }

            if (!isDisposed)
            {
                view.SetSurveyAvailable(availableSurveys.Count > 0);
            }
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
            view.SurveyClicked -= OnSurveyClicked;
        }

        private void OnPlayClicked()
        {
            navigator.GoToGame();
        }

        private void OnBackToTitleClicked()
        {
            navigator.GoToTitle();
        }

        private void OnSurveyClicked()
        {
            if (availableSurveys.Count > 0)
            {
                SurveyRequested?.Invoke(availableSurveys[0]);
            }
        }
    }
}
