using ArcherLegend.Application;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class ArcherGameSessionTests
    {
        [Test]
        public void CreateRunResult_撃破数とスコアとウェーブを返す()
        {
            using (var session = new ArcherGameSession())
            {
                session.RegisterEnemyDefeated(session.CreateEnemy());
                session.RegisterWaveCleared();
                session.RegisterEnemyDefeated(session.CreateEnemy());

                var run = session.CreateRunResult();

                Assert.That(run.EnemiesDefeated, Is.EqualTo(2));
                Assert.That(run.Score, Is.EqualTo(10 * 1 + 10 * 2));
                Assert.That(run.Wave, Is.EqualTo(2));
                Assert.That(run.Level, Is.EqualTo(session.Player.Level));
            }
        }

        [Test]
        public void Restart_ラン単位の値をリセットする()
        {
            using (var session = new ArcherGameSession())
            {
                session.RegisterEnemyDefeated(session.CreateEnemy());
                session.RegisterWaveCleared();

                session.Restart();

                var run = session.CreateRunResult();
                Assert.That(run.EnemiesDefeated, Is.Zero);
                Assert.That(run.Score, Is.Zero);
                Assert.That(run.Wave, Is.EqualTo(1));
            }
        }
    }
}
