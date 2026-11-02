using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
        /// 同期中に増えた結果も、実行中の同期で続けて送ります。
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
            try
            {
                await SyncAsync();
            }
            catch (GameApiException e) when (e.StatusCode != 0)
            {
                // サーバーには届いているので、結果を送れなくてもランキングは表示する
                Debug.LogWarning($"Failed to send run results before loading ranking: {e.Message}");
            }

            var profile = await progress.LoadProfileAsync();
            return await api.GetRankingAsync(profile.Id, limit, CancellationToken.None);
        }

        /// <summary>未登録ならプレイヤーを登録します。プレイ結果は送りません。</summary>
        public async Task<PlayerProfile> EnsureRegisteredAsync()
        {
            var profile = await progress.LoadProfileAsync();
            if (!profile.IsRegistered)
            {
                await api.RegisterPlayerAsync(profile.Id, profile.Name, CancellationToken.None);
                await progress.SetRegisteredAsync(true);
            }

            return profile;
        }

        private async Task SyncCoreAsync()
        {
            var profile = await EnsureRegisteredAsync();

            // 送信中に増えた結果も送れるよう、1 件送るごとに送信待ちを読み直す
            var attempted = new HashSet<string>();
            while (true)
            {
                var run = (await progress.GetPendingRunsAsync()).FirstOrDefault(pending => attempted.Add(pending.runId));
                if (run == null)
                {
                    return;
                }

                await SubmitRunAsync(profile.Id, run);
            }
        }

        private async Task SubmitRunAsync(string playerId, PendingRun run)
        {
            try
            {
                await api.SubmitRunAsync(playerId, run, CancellationToken.None);
            }
            catch (GameApiException e) when (e.IsNotFound)
            {
                // サーバー側からプレイヤーが消えていた（開発中に DB を作り直した場合など）。次回の同期で登録し直す
                await progress.SetRegisteredAsync(false);
                throw;
            }
            catch (GameApiException e) when (e.IsConflict)
            {
                // 同時更新なら次回の送信で成功するので残す。この結果のせいで後の結果を止めないよう、先へ進む
                Debug.LogWarning($"Run {run.runId} conflicted and will be retried later: {e.Message}");
                return;
            }
            catch (GameApiException e) when (!e.IsRetryable)
            {
                // サーバーが受け付けない結果は、送り直しても成功しない。ローカルの戦績には反映済みなので送信待ちから外す
                Debug.LogWarning($"Run {run.runId} was rejected and will not be sent again: {e.Message}");
                await progress.CompleteRunAsync(run.runId);
                return;
            }

            await progress.CompleteRunAsync(run.runId);
        }
    }
}
