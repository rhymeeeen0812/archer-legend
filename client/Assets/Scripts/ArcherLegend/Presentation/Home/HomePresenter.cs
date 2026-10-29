using System;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using UnityEngine;

namespace ArcherLegend.Presentation.Home
{
    public sealed class HomePresenter : IDisposable
    {
        private readonly IHomeView view;
        private readonly PlayerProgressService progress;
        private readonly ISceneNavigator navigator;
        private bool isDisposed;

        public HomePresenter(IHomeView view, PlayerProgressService progress, ISceneNavigator navigator)
        {
            this.view = view;
            this.progress = progress;
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
