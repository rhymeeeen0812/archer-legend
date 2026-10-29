using System;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class PlayerProgressServiceTests
    {
        [Test]
        public async Task LoadAsync_保存データがなければ新しいUserDataを返す()
        {
            var service = new PlayerProgressService(new InMemorySaveDataRepository());

            var userData = await service.LoadAsync();

            Assert.That(userData.PlayCount, Is.Zero);
        }

        [Test]
        public async Task LoadAsync_2回目以降は同じインスタンスを返す()
        {
            var service = new PlayerProgressService(new InMemorySaveDataRepository());

            var first = await service.LoadAsync();
            var second = await service.LoadAsync();

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public async Task LoadAsync_失敗した後は再試行する()
        {
            var repository = new InMemorySaveDataRepository { LoadException = new InvalidOperationException() };
            var service = new PlayerProgressService(repository);
            Assert.That(async () => await service.LoadAsync(), Throws.InvalidOperationException);

            repository.LoadException = null;
            repository.Stored = new SaveData { bestScore = 50 };

            var userData = await service.LoadAsync();
            Assert.That(userData.BestScore, Is.EqualTo(50));
        }

        [Test]
        public async Task SaveAsync_記録したUserDataを保存先に書き込む()
        {
            var repository = new InMemorySaveDataRepository();
            var service = new PlayerProgressService(repository);
            await service.RecordRunAsync(new RunResult(score: 90, wave: 4, enemiesDefeated: 6, level: 2));

            await service.SaveAsync();

            Assert.That(repository.Stored.bestScore, Is.EqualTo(90));
            Assert.That(repository.Stored.bestWave, Is.EqualTo(4));
            Assert.That(repository.Stored.playCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RecordRunAsync_結果をサーバーへの送信待ちとして保存する()
        {
            var repository = new InMemorySaveDataRepository();
            var service = new PlayerProgressService(repository);

            await service.RecordRunAsync(new RunResult(score: 90, wave: 4, enemiesDefeated: 6, level: 2));
            await service.SaveAsync();

            var pending = repository.Stored.pendingRuns;
            Assert.That(pending, Has.Count.EqualTo(1));
            Assert.That(Guid.TryParse(pending[0].runId, out _), Is.True);
            Assert.That(pending[0].score, Is.EqualTo(90));
        }

        [Test]
        public async Task LoadProfileAsync_初回はIdと仮の名前を作り保存後も同じIdを使う()
        {
            var repository = new InMemorySaveDataRepository();
            var service = new PlayerProgressService(repository);
            var first = await service.LoadProfileAsync();
            await service.SaveAsync();

            var reloaded = await new PlayerProgressService(repository).LoadProfileAsync();

            Assert.That(Guid.TryParse(first.Id, out _), Is.True);
            Assert.That(first.Name, Does.StartWith("アーチャー"));
            Assert.That(first.IsRegistered, Is.False);
            Assert.That(reloaded.Id, Is.EqualTo(first.Id));
        }

        [Test]
        public async Task LoadAsync_プレイヤーIdのない古いセーブデータも戦績を引き継ぐ()
        {
            var repository = new InMemorySaveDataRepository
            {
                Stored = new SaveData { version = 1, bestScore = 70, playCount = 3, pendingRuns = null }
            };
            var service = new PlayerProgressService(repository);

            var userData = await service.LoadAsync();
            var profile = await service.LoadProfileAsync();

            Assert.That(userData.BestScore, Is.EqualTo(70));
            Assert.That(profile.Id, Is.Not.Empty);
            Assert.That(await service.GetPendingRunsAsync(), Is.Empty);
        }
    }
}
