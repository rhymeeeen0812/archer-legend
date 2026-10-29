using System;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using UnityEngine;

namespace ArcherLegend.Presentation.Result
{
    public sealed class ResultPresenter : IDisposable
    {
        private readonly IResultView view;
        private readonly PlayerProgressService progress;
        private readonly OnlineSyncService sync;
        private readonly ISceneNavigator navigator;
        private readonly Action retry;
        private bool isDisposed;

        public ResultPresenter(IResultView view, PlayerProgressService progress, OnlineSyncService sync, ISceneNavigator navigator, Action retry)
        {
            this.view = view;
            this.progress = progress;
            this.sync = sync;
            this.navigator = navigator;
            this.retry = retry;
            view.RetryClicked += OnRetryClicked;
            view.HomeClicked += OnHomeClicked;
        }

        /// <summary>
        /// ランの結果を記録・保存してからリザルトを表示します。保存に失敗しても結果は表示します。
        /// サーバーへの送信は表示の後に行い、通信を待たずに次のプレイへ進めるようにします。
        /// </summary>
        public async Task ShowAsync(RunResult run)
        {
            var record = default(RunRecordResult);
            var isSaved = false;
            try
            {
                record = await progress.RecordRunAsync(run);
                await progress.SaveAsync();
                isSaved = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (isDisposed)
            {
                return;
            }

            view.Show(run.Score, run.Wave, run.EnemiesDefeated, run.Level, record.IsNewBestScore, record.IsNewBestWave);
            if (!isSaved)
            {
                view.ShowSaveError();
                return;
            }

            try
            {
                await sync.SyncAsync();
            }
            catch (Exception e)
            {
                // 送れなかった結果はセーブデータに残り、次の同期（ホーム画面など）で送り直す
                Debug.LogWarning($"Failed to send run result: {e.Message}");
            }
        }

        public void Dispose()
        {
            isDisposed = true;
            view.RetryClicked -= OnRetryClicked;
            view.HomeClicked -= OnHomeClicked;
        }

        private void OnRetryClicked()
        {
            view.Hide();
            retry();
        }

        private void OnHomeClicked()
        {
            navigator.GoToHome();
        }
    }
}
