using ArcherLegend.Domain;
using NUnit.Framework;

namespace ArcherLegend.Tests
{
    public class PlayerTests
    {
        [Test]
        public void TryTakeHit_ダメージを受けると無敵時間が始まる()
        {
            using (var player = new Player())
            {
                var hit = player.TryTakeHit(10);

                Assert.That(hit, Is.True);
                Assert.That(player.Health, Is.EqualTo(90));
                Assert.That(player.IsInvincible, Is.True);
            }
        }

        [Test]
        public void TryTakeHit_無敵時間中はダメージを受けない()
        {
            using (var player = new Player())
            {
                player.TryTakeHit(10);

                var hit = player.TryTakeHit(10);

                Assert.That(hit, Is.False);
                Assert.That(player.Health, Is.EqualTo(90));
            }
        }

        [Test]
        public void TickInvincibility_無敵時間が過ぎると再びダメージを受ける()
        {
            using (var player = new Player())
            {
                player.TryTakeHit(10);

                player.TickInvincibility(Player.InvincibleDuration);

                Assert.That(player.IsInvincible, Is.False);
                Assert.That(player.TryTakeHit(10), Is.True);
                Assert.That(player.Health, Is.EqualTo(80));
            }
        }

        [Test]
        public void AddExperience_必要な経験値を超えた分だけレベルが上がり余りを残す()
        {
            using (var player = new Player())
            {
                var levels = player.AddExperience(6 + 12 + 1);

                Assert.That(levels, Is.EqualTo(2));
                Assert.That(player.Level, Is.EqualTo(3));
                Assert.That(player.Experience, Is.EqualTo(1));
                Assert.That(player.Experience, Is.LessThan(player.ExperienceToNext));
            }
        }

        [Test]
        public void AddExperience_必要な経験値に届かなければレベルは上がらない()
        {
            using (var player = new Player())
            {
                Assert.That(player.AddExperience(5), Is.Zero);
                Assert.That(player.Level, Is.EqualTo(1));
            }
        }

        [Test]
        public void ApplyUpgrade_拡散矢は2本ずつ増えて上限で止まる()
        {
            using (var player = new Player())
            {
                player.ApplyUpgrade(UpgradeType.SpreadShot);
                Assert.That(player.ArrowCount, Is.EqualTo(3));

                player.ApplyUpgrade(UpgradeType.SpreadShot);
                player.ApplyUpgrade(UpgradeType.SpreadShot);
                Assert.That(player.ArrowCount, Is.EqualTo(Player.MaxArrowCount));
            }
        }

        [Test]
        public void ApplyUpgrade_貫通矢と跳弾は1回ずつ増えて上限で止まる()
        {
            using (var player = new Player())
            {
                player.ApplyUpgrade(UpgradeType.PiercingShot);
                player.ApplyUpgrade(UpgradeType.BouncingShot);
                Assert.That(player.PierceCount, Is.EqualTo(1));
                Assert.That(player.BounceCount, Is.EqualTo(1));

                for (var i = 0; i < 5; i++)
                {
                    player.ApplyUpgrade(UpgradeType.PiercingShot);
                    player.ApplyUpgrade(UpgradeType.BouncingShot);
                }

                Assert.That(player.PierceCount, Is.EqualTo(Player.MaxPierceCount));
                Assert.That(player.BounceCount, Is.EqualTo(Player.MaxBounceCount));
            }
        }

        [Test]
        public void CanUpgrade_上限に達した攻撃パターンだけ強化できなくなる()
        {
            using (var player = new Player())
            {
                for (var i = 0; i < Player.MaxPierceCount; i++)
                {
                    player.ApplyUpgrade(UpgradeType.PiercingShot);
                }

                Assert.That(player.CanUpgrade(UpgradeType.PiercingShot), Is.False);
                Assert.That(player.CanUpgrade(UpgradeType.SpreadShot), Is.True);
                Assert.That(player.CanUpgrade(UpgradeType.BouncingShot), Is.True);
                Assert.That(player.CanUpgrade(UpgradeType.PowerShot), Is.True);
            }
        }

        [Test]
        public void ResetForNewRun_攻撃パターンを初期状態に戻す()
        {
            using (var player = new Player())
            {
                player.ApplyUpgrade(UpgradeType.SpreadShot);
                player.ApplyUpgrade(UpgradeType.PiercingShot);
                player.ApplyUpgrade(UpgradeType.BouncingShot);

                player.ResetForNewRun();

                Assert.That(player.ArrowCount, Is.EqualTo(1));
                Assert.That(player.PierceCount, Is.Zero);
                Assert.That(player.BounceCount, Is.Zero);
            }
        }

        [Test]
        public void ResetForNewRun_無敵時間を解除する()
        {
            using (var player = new Player())
            {
                player.TryTakeHit(10);

                player.ResetForNewRun();

                Assert.That(player.IsInvincible, Is.False);
            }
        }
    }
}
