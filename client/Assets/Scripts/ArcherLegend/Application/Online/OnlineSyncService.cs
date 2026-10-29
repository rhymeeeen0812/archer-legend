using System.Threading;
using System.Threading.Tasks;

namespace ArcherLegend.Application
{
    /// <summary>
    /// ローカルのセーブデータをサーバーへ反映するユースケースです。
    /// ゲームはオフラインでも遊べるようにし、プレイ結果は送れるようになった時点でまとめて送ります。
    /// </summary>
    public sealed class OnlineSyncService
    {
        private readonly PlayerProgressService progress;
        private readonly IGameApiClient api;
        private Task syncTask;

        public OnlineSyncService(PlayerProgressService progress, IGameApiClient api)
        {
            this.progress = progress;
            this.api = api;
        }

        /// <summary>
        /// 未登録ならプレイヤーを登録し、送信待ちのプレイ結果を古い順に送ります。
        /// 同期中に呼ばれた場合は、実行中の同期の完了を待ちます（同じ結果を同時に送らないように）。
        /// </summary>
        public Task SyncAsync()
        {
            if (syncTask == null || syncTask.IsCompleted)
            {
                syncTask = SyncCoreAsync();
            }

            return syncTask;
        }

        /// <summary>最新の結果を反映してから、ランキングを取得します。</summary>
        public async Task<Ranking> GetRankingAsync(int limit)
        {
            await SyncAsync();
            var profile = await progress.LoadProfileAsync();
            return await api.GetRankingAsync(profile.Id, limit, CancellationToken.None);
        }

        private async Task SyncCoreAsync()
        {
            var profile = await progress.LoadProfileAsync();
            if (!profile.IsRegistered)
            {
                await api.RegisterPlayerAsync(profile.Id, profile.Name, CancellationToken.None);
                await progress.SetRegisteredAsync(true);
            }

            foreach (var run in await progress.GetPendingRunsAsync())
            {
                try
                {
                    await api.SubmitRunAsync(profile.Id, run, CancellationToken.None);
                }
                catch (GameApiException e) when (e.IsNotFound)
                {
                    // サーバー側からプレイヤーが消えていた（開発中に DB を作り直した場合など）。次回の同期で登録し直す
                    await progress.SetRegisteredAsync(false);
                    throw;
                }

                await progress.CompleteRunAsync(run.runId);
            }
        }
    }
}
