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
        public void ApplyUpgrade_選択中にレベルが上がった分も続けて選べる()
        {
            using (var session = new ArcherGameSession())
            {
                while (session.Player.Level < 3)
                {
                    session.RegisterEnemyDefeated(session.CreateEnemy());
                }

                session.ApplyUpgrade(session.CurrentUpgradeOptions[0].Type);
                Assert.That(session.IsUpgradePending, Is.True);
                Assert.That(session.CurrentUpgradeOptions, Is.Not.Empty);

                session.ApplyUpgrade(session.CurrentUpgradeOptions[0].Type);
                Assert.That(session.IsUpgradePending, Is.False);
                Assert.That(session.CurrentUpgradeOptions, Is.Empty);
            }
        }

        [Test]
        public void RegisterEnemyDefeated_選択中にレベルが上がっても表示中の選択肢を変えない()
        {
            using (var session = new ArcherGameSession())
            {
                while (!session.IsUpgradePending)
                {
                    session.RegisterEnemyDefeated(session.CreateEnemy());
                }

                var shown = session.CurrentUpgradeOptions;
                while (session.Player.Level < 3)
                {
                    session.RegisterEnemyDefeated(session.CreateEnemy());
                }

                Assert.That(session.CurrentUpgradeOptions, Is.SameAs(shown));
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
