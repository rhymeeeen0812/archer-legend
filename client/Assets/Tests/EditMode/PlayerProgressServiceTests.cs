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
            var userData = await service.LoadAsync();
            userData.RecordRun(new RunResult(score: 90, wave: 4, enemiesDefeated: 6, level: 2));

            await service.SaveAsync();

            Assert.That(repository.Stored.bestScore, Is.EqualTo(90));
            Assert.That(repository.Stored.bestWave, Is.EqualTo(4));
            Assert.That(repository.Stored.playCount, Is.EqualTo(1));
        }
    }
}
