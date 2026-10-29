using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArcherLegend.Tests
{
    public class JsonFileSaveDataRepositoryTests
    {
        private string directory;
        private JsonFileSaveDataRepository repository;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ArcherLegendTests", Path.GetRandomFileName());
            repository = new JsonFileSaveDataRepository(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public async Task LoadAsync_ファイルがなければnullを返す()
        {
            Assert.That(await repository.LoadAsync(CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task SaveAsync_保存した内容をLoadAsyncで読み戻せる()
        {
            await repository.SaveAsync(new SaveData { bestScore = 300, bestWave = 7, playCount = 4 }, CancellationToken.None);

            var loaded = await repository.LoadAsync(CancellationToken.None);

            Assert.That(loaded.bestScore, Is.EqualTo(300));
            Assert.That(loaded.bestWave, Is.EqualTo(7));
            Assert.That(loaded.playCount, Is.EqualTo(4));
            Assert.That(loaded.version, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public async Task LoadAsync_本体がなく一時ファイルだけ残っていれば復旧する()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "save.json.tmp"), JsonUtility.ToJson(new SaveData { bestScore = 42 }));

            var loaded = await repository.LoadAsync(CancellationToken.None);

            Assert.That(loaded.bestScore, Is.EqualTo(42));
        }

        [Test]
        public async Task LoadAsync_壊れたファイルは退避してnullを返す()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "save.json"), "{ broken");
            LogAssert.Expect(LogType.Warning, new Regex("Save data is broken"));

            var loaded = await repository.LoadAsync(CancellationToken.None);

            Assert.That(loaded, Is.Null);
            Assert.That(File.Exists(Path.Combine(directory, "save.json.broken")), Is.True);
            Assert.That(File.Exists(Path.Combine(directory, "save.json")), Is.False);
        }
    }
}
