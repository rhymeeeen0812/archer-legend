using System.Linq;
using ArcherLegend.Application;
using ArcherLegend.Domain;
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
        public void ComposeWave_今のウェーブの顔ぶれを返し指定した種類の敵を作る()
        {
            using (var session = new ArcherGameSession())
            {
                for (var i = 1; i < EnemyWave.FirstChargerWave; i++)
                {
                    session.RegisterWaveCleared();
                }

                Assert.That(session.ComposeWave(), Is.EqualTo(EnemyWave.Compose(EnemyWave.FirstChargerWave)));

                var enemy = session.CreateEnemy(EnemyType.Charger);
                Assert.That(enemy.Type, Is.EqualTo(EnemyType.Charger));
            }
        }

        [Test]
        public void RegisterEnemyDefeated_上限に達した強化は選択肢に出ない()
        {
            using (var session = new ArcherGameSession())
            {
                for (var i = 0; i < Player.MaxPierceCount; i++)
                {
                    session.Player.ApplyUpgrade(UpgradeType.PiercingShot);
                    session.Player.ApplyUpgrade(UpgradeType.BouncingShot);
                    session.Player.ApplyUpgrade(UpgradeType.SpreadShot);
                }

                // 選択肢はランダムなので、何度もレベルを上げて一度も出ないことを確かめる
                for (var level = 0; level < 10; level++)
                {
                    while (!session.IsUpgradePending)
                    {
                        session.RegisterEnemyDefeated(session.CreateEnemy());
                    }

                    var types = session.CurrentUpgradeOptions.Select(option => option.Type).ToArray();
                    Assert.That(types, Has.Length.EqualTo(3));
                    Assert.That(types, Is.Unique);
                    Assert.That(types, Has.None.EqualTo(UpgradeType.PiercingShot));
                    Assert.That(types, Has.None.EqualTo(UpgradeType.BouncingShot));
                    Assert.That(types, Has.None.EqualTo(UpgradeType.SpreadShot));

                    session.ApplyUpgrade(types[0]);
                }
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
