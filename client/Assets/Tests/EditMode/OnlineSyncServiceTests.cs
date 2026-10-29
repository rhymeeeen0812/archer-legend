using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class OnlineSyncServiceTests
    {
        [Test]
        public async Task SyncAsync_未登録なら登録してから送信待ちを古い順に送る()
        {
            var (progress, repository) = await CreateProgressAsync(runCount: 2);
            var pending = await progress.GetPendingRunsAsync();
            var api = new FakeGameApiClient();

            await new OnlineSyncService(progress, api).SyncAsync();

            var profile = await progress.LoadProfileAsync();
            Assert.That(api.RegisteredIds, Is.EqualTo(new[] { profile.Id }));
            Assert.That(api.SubmittedRunIds, Is.EqualTo(new[] { pending[0].runId, pending[1].runId }));
            Assert.That(repository.Stored.isRegistered, Is.True);
            Assert.That(repository.Stored.pendingRuns, Is.Empty);
        }

        [Test]
        public async Task SyncAsync_登録済みなら登録し直さない()
        {
            var (progress, _) = await CreateProgressAsync(runCount: 0);
            var api = new FakeGameApiClient();
            var sync = new OnlineSyncService(progress, api);

            await sync.SyncAsync();
            await sync.SyncAsync();

            Assert.That(api.RegisteredIds, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SyncAsync_送信に失敗したら送れなかった結果を送信待ちに残す()
        {
            var (progress, repository) = await CreateProgressAsync(runCount: 2);
            var pending = await progress.GetPendingRunsAsync();
            var api = new FakeGameApiClient
            {
                SubmitFailure = run => run.runId == pending[1].runId ? new GameApiException(0, "offline") : null
            };

            Assert.That(async () => await new OnlineSyncService(progress, api).SyncAsync(), Throws.TypeOf<GameApiException>());

            Assert.That(api.SubmittedRunIds, Is.EqualTo(new[] { pending[0].runId }));
            Assert.That(repository.Stored.pendingRuns.ConvertAll(run => run.runId), Is.EqualTo(new List<string> { pending[1].runId }));
        }

        [Test]
        public async Task SyncAsync_サーバーにプレイヤーがいなければ次回登録し直す()
        {
            var (progress, _) = await CreateProgressAsync(runCount: 1);
            var api = new FakeGameApiClient();
            var sync = new OnlineSyncService(progress, api);
            await progress.SetRegisteredAsync(true);
            api.SubmitFailure = _ => new GameApiException(404, "not found");

            Assert.That(async () => await sync.SyncAsync(), Throws.TypeOf<GameApiException>());
            api.SubmitFailure = null;
            await sync.SyncAsync();

            Assert.That(api.RegisteredIds, Has.Count.EqualTo(1));
            Assert.That(api.SubmittedRunIds, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SyncAsync_同期中に呼ばれても同じ結果を二重に送らない()
        {
            var (progress, _) = await CreateProgressAsync(runCount: 1);
            var api = new FakeGameApiClient { PendingSubmit = new TaskCompletionSource<bool>() };
            var sync = new OnlineSyncService(progress, api);

            var first = sync.SyncAsync();
            var second = sync.SyncAsync();
            api.PendingSubmit.SetResult(true);
            await Task.WhenAll(first, second);

            Assert.That(api.SubmittedRunIds, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetRankingAsync_送信待ちを送ってからランキングを取得する()
        {
            var (progress, _) = await CreateProgressAsync(runCount: 1);
            var api = new FakeGameApiClient
            {
                Ranking = new Ranking(Array.Empty<RankingEntry>(), new RankingEntry(3, "自分", 100, 2, true))
            };

            var ranking = await new OnlineSyncService(progress, api).GetRankingAsync(5);

            Assert.That(api.SubmittedRunIds, Has.Count.EqualTo(1));
            Assert.That(ranking.Me?.Rank, Is.EqualTo(3));
        }

        private static async Task<(PlayerProgressService progress, InMemorySaveDataRepository repository)> CreateProgressAsync(int runCount)
        {
            var repository = new InMemorySaveDataRepository();
            var progress = new PlayerProgressService(repository);
            for (var i = 0; i < runCount; i++)
            {
                await progress.RecordRunAsync(new RunResult(score: 10 * (i + 1), wave: 1, enemiesDefeated: 1, level: 1));
            }

            await progress.SaveAsync();
            return (progress, repository);
        }
    }
}
