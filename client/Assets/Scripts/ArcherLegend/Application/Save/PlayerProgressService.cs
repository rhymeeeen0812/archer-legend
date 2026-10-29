using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Domain;

namespace ArcherLegend.Application
{
    /// <summary>
    /// シーンをまたいで 1 つの UserData とプレイヤー情報を共有し、保存先とのやり取りをまとめるユースケースです。
    /// </summary>
    public sealed class PlayerProgressService
    {
        private const string DefaultNamePrefix = "アーチャー";

        private readonly ISaveDataRepository repository;
        private Task<State> loadTask;

        public PlayerProgressService(ISaveDataRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// 初回だけ保存先から読み込み、以降は同じ UserData を返します。読み込みに失敗した場合は次回呼び出し時に再試行します。
        /// </summary>
        public async Task<UserData> LoadAsync()
        {
            return (await LoadStateAsync()).UserData;
        }

        public async Task<PlayerProfile> LoadProfileAsync()
        {
            return (await LoadStateAsync()).Profile;
        }

        /// <summary>
        /// ランの結果を戦績に反映し、サーバーへの送信待ちに加えます。保存は <see cref="SaveAsync"/> で行います。
        /// </summary>
        public async Task<RunRecordResult> RecordRunAsync(RunResult run)
        {
            var state = await LoadStateAsync();
            var record = state.UserData.RecordRun(run);
            state.PendingRuns.Add(new PendingRun
            {
                runId = Guid.NewGuid().ToString(),
                score = run.Score,
                wave = run.Wave,
                enemiesDefeated = run.EnemiesDefeated,
                level = run.Level
            });
            return record;
        }

        /// <summary>送信待ちのプレイ結果を古い順に返します。</summary>
        public async Task<IReadOnlyList<PendingRun>> GetPendingRunsAsync()
        {
            return (await LoadStateAsync()).PendingRuns.ToArray();
        }

        /// <summary>サーバーへ送れたプレイ結果を送信待ちから外して保存します。</summary>
        public async Task CompleteRunAsync(string runId)
        {
            var state = await LoadStateAsync();
            state.PendingRuns.RemoveAll(run => run.runId == runId);
            await SaveAsync();
        }

        public async Task SetRegisteredAsync(bool isRegistered)
        {
            var state = await LoadStateAsync();
            state.Profile.IsRegistered = isRegistered;
            await SaveAsync();
        }

        public async Task SaveAsync()
        {
            var state = await LoadStateAsync();
            var userData = state.UserData;
            var data = new SaveData
            {
                bestScore = userData.BestScore,
                bestWave = userData.BestWave,
                totalScore = userData.TotalScore,
                totalEnemiesDefeated = userData.TotalEnemiesDefeated,
                playCount = userData.PlayCount,
                playerId = state.Profile.Id,
                playerName = state.Profile.Name,
                isRegistered = state.Profile.IsRegistered,
                pendingRuns = new List<PendingRun>(state.PendingRuns)
            };
            await repository.SaveAsync(data, CancellationToken.None);
        }

        private Task<State> LoadStateAsync()
        {
            if (loadTask == null || loadTask.IsFaulted || loadTask.IsCanceled)
            {
                loadTask = LoadCoreAsync();
            }

            return loadTask;
        }

        private async Task<State> LoadCoreAsync()
        {
            var data = await repository.LoadAsync(CancellationToken.None) ?? new SaveData();
            return new State
            {
                UserData = data.ToUserData(),
                Profile = CreateProfile(data),
                PendingRuns = data.pendingRuns ?? new List<PendingRun>()
            };
        }

        // 初めて起動したとき（または古いセーブデータ）は Id を新しく作る。
        // 作った Id は、サーバーへの登録やプレイ結果の記録で保存されたときに確定する
        private static PlayerProfile CreateProfile(SaveData data)
        {
            if (!string.IsNullOrEmpty(data.playerId))
            {
                return new PlayerProfile(data.playerId, data.playerName, data.isRegistered);
            }

            var id = Guid.NewGuid();
            var suffix = (id.GetHashCode() & 0x7fffffff) % 10000;
            return new PlayerProfile(id.ToString(), $"{DefaultNamePrefix}{suffix:D4}", false);
        }

        private sealed class State
        {
            public UserData UserData;
            public PlayerProfile Profile;
            public List<PendingRun> PendingRuns;
        }
    }
}
