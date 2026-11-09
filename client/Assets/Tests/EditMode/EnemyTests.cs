using System.Linq;
using ArcherLegend.Domain;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class EnemyTests
    {
        [Test]
        public void Enemy_射撃する敵だけが弾のダメージと撃つ間隔を持つ()
        {
            using (var chaser = new Enemy(3, EnemyType.Chaser))
            using (var shooter = new Enemy(3, EnemyType.Shooter))
            using (var charger = new Enemy(3, EnemyType.Charger))
            {
                Assert.That(shooter.ShotDamage, Is.GreaterThan(0));
                Assert.That(shooter.ShotInterval, Is.GreaterThan(0f));
                Assert.That(chaser.ShotDamage, Is.Zero);
                Assert.That(charger.ShotDamage, Is.Zero);
            }
        }

        [Test]
        public void Enemy_射撃する敵は打たれ弱く突進する敵は打たれ強い()
        {
            using (var chaser = new Enemy(4, EnemyType.Chaser))
            using (var shooter = new Enemy(4, EnemyType.Shooter))
            using (var charger = new Enemy(4, EnemyType.Charger))
            {
                Assert.That(shooter.MaxHealth, Is.LessThan(chaser.MaxHealth));
                Assert.That(charger.MaxHealth, Is.GreaterThan(chaser.MaxHealth));
                Assert.That(charger.ContactDamage, Is.GreaterThan(chaser.ContactDamage));
            }
        }

        [Test]
        public void Enemy_ウェーブが進むと射撃の間隔が短くなり下限で止まる()
        {
            using (var early = new Enemy(3, EnemyType.Shooter))
            using (var late = new Enemy(10, EnemyType.Shooter))
            using (var veryLate = new Enemy(100, EnemyType.Shooter))
            {
                Assert.That(late.ShotInterval, Is.LessThan(early.ShotInterval));
                Assert.That(veryLate.ShotInterval, Is.EqualTo(1.4f).Within(0.0001f));
            }
        }

        [Test]
        public void Compose_敵の数はこれまでどおりウェーブ数に3を足した数()
        {
            for (var wave = 1; wave <= 30; wave++)
            {
                Assert.That(EnemyWave.Compose(wave), Has.Length.EqualTo(3 + wave), $"ウェーブ {wave}");
            }
        }

        [Test]
        public void Compose_序盤は追跡する敵だけが出る()
        {
            for (var wave = 1; wave < EnemyWave.FirstShooterWave; wave++)
            {
                Assert.That(EnemyWave.Compose(wave), Is.All.EqualTo(EnemyType.Chaser), $"ウェーブ {wave}");
            }
        }

        [Test]
        public void Compose_射撃する敵と突進する敵はそれぞれ決まったウェーブから出る()
        {
            var firstShooterWave = EnemyWave.Compose(EnemyWave.FirstShooterWave);
            Assert.That(firstShooterWave.Count(type => type == EnemyType.Shooter), Is.EqualTo(1));
            Assert.That(firstShooterWave, Has.None.EqualTo(EnemyType.Charger));

            var firstChargerWave = EnemyWave.Compose(EnemyWave.FirstChargerWave);
            Assert.That(firstChargerWave.Count(type => type == EnemyType.Charger), Is.EqualTo(1));
        }

        [Test]
        public void Compose_どのウェーブでも追跡する敵が3分の1以上残る()
        {
            for (var wave = 1; wave <= 50; wave++)
            {
                var types = EnemyWave.Compose(wave);
                var chasers = types.Count(type => type == EnemyType.Chaser);
                Assert.That(chasers * 3, Is.GreaterThanOrEqualTo(types.Length), $"ウェーブ {wave}");
            }
        }

        [Test]
        public void Compose_種類を混ぜて並べ同じ種類の特殊な敵を続けて出さない()
        {
            var types = EnemyWave.Compose(12);

            for (var i = 1; i < types.Length; i++)
            {
                if (types[i] != EnemyType.Chaser)
                {
                    Assert.That(types[i], Is.Not.EqualTo(types[i - 1]), $"{i} 番目");
                }
            }
        }

        [Test]
        public void ChargeCycle_遠くにいる間は近づくだけ()
        {
            var cycle = new ChargeCycle();

            Assert.That(cycle.Tick(1f, ChargeCycle.TriggerDistance + 1f), Is.EqualTo(ChargePhase.Approach));
            Assert.That(cycle.Tick(1f, ChargeCycle.TriggerDistance + 1f), Is.EqualTo(ChargePhase.Approach));
        }

        [Test]
        public void ChargeCycle_近づくと溜めて突進し休んでからまた近づく()
        {
            var cycle = new ChargeCycle();

            Assert.That(cycle.Tick(0.1f, ChargeCycle.TriggerDistance), Is.EqualTo(ChargePhase.Windup));

            // 溜めの途中でプレイヤーが離れても、突進は止めない
            Assert.That(cycle.Tick(ChargeCycle.WindupDuration * 0.5f, 99f), Is.EqualTo(ChargePhase.Windup));
            Assert.That(cycle.Tick(ChargeCycle.WindupDuration * 0.5f, 99f), Is.EqualTo(ChargePhase.Dash));

            Assert.That(cycle.Tick(ChargeCycle.DashDuration, 1f), Is.EqualTo(ChargePhase.Recover));

            // 休みの間はすぐ近くにいても次の突進をしない
            Assert.That(cycle.Tick(ChargeCycle.RecoverDuration * 0.5f, 1f), Is.EqualTo(ChargePhase.Recover));
            Assert.That(cycle.Tick(ChargeCycle.RecoverDuration * 0.5f, 1f), Is.EqualTo(ChargePhase.Approach));
        }
    }
}
