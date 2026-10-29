using ArcherLegend.Domain;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class UserDataTests
    {
        [Test]
        public void RecordRun_初回は自己ベストと累計を更新する()
        {
            var userData = new UserData();

            var record = userData.RecordRun(new RunResult(score: 120, wave: 3, enemiesDefeated: 8, level: 2));

            Assert.That(record.IsNewBestScore, Is.True);
            Assert.That(record.IsNewBestWave, Is.True);
            Assert.That(userData.BestScore, Is.EqualTo(120));
            Assert.That(userData.BestWave, Is.EqualTo(3));
            Assert.That(userData.TotalScore, Is.EqualTo(120));
            Assert.That(userData.TotalEnemiesDefeated, Is.EqualTo(8));
            Assert.That(userData.PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void RecordRun_自己ベスト未満なら累計だけ更新する()
        {
            var userData = new UserData(bestScore: 200, bestWave: 5, totalScore: 200, totalEnemiesDefeated: 10, playCount: 1);

            var record = userData.RecordRun(new RunResult(score: 150, wave: 5, enemiesDefeated: 7, level: 3));

            Assert.That(record.IsNewBestScore, Is.False);
            Assert.That(record.IsNewBestWave, Is.False, "同じウェーブは更新扱いにしない");
            Assert.That(userData.BestScore, Is.EqualTo(200));
            Assert.That(userData.TotalScore, Is.EqualTo(350));
            Assert.That(userData.TotalEnemiesDefeated, Is.EqualTo(17));
            Assert.That(userData.PlayCount, Is.EqualTo(2));
        }

        [Test]
        public void コンストラクタ_負の値は0に丸める()
        {
            var userData = new UserData(-1, -1, -1, -1, -1);

            Assert.That(userData.BestScore, Is.Zero);
            Assert.That(userData.PlayCount, Is.Zero);
        }
    }
}
