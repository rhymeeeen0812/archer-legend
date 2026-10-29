using System;
using System.Collections.Generic;
using ArcherLegend.Domain;

namespace ArcherLegend.Application
{
    /// <summary>
    /// アプリケーション層のユースケースです。ランの生成・破棄を管理し、Domain の明示的な Dispose を担当します。
    /// </summary>
    public sealed class ArcherGameSession : IDisposable
    {
        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly System.Random random = new System.Random();
        private bool isDisposed;

        public Player Player { get; } = new Player();
        public UserData UserData { get; } = new UserData();
        public int Wave { get; private set; } = 1;
        public int Score { get; private set; }
        public bool IsUpgradePending { get; private set; }
        public bool IsGameOver => !Player.IsAlive;

        public UpgradeOption[] CurrentUpgradeOptions { get; private set; } = Array.Empty<UpgradeOption>();

        public Enemy CreateEnemy()
        {
            ThrowIfDisposed();
            var enemy = new Enemy(Wave);
            activeEnemies.Add(enemy);
            return enemy;
        }

        public void RegisterEnemyDefeated(Enemy enemy)
        {
            ThrowIfDisposed();
            var gainedScore = 10 * Wave;
            Score += gainedScore;
            UserData.RegisterEnemyDefeated(gainedScore, Wave);

            if (Player.AddExperience(enemy.ExperienceReward))
            {
                IsUpgradePending = true;
                CurrentUpgradeOptions = RollUpgradeOptions();
            }

            activeEnemies.Remove(enemy);
            enemy.Dispose();
        }

        public void RegisterWaveCleared()
        {
            ThrowIfDisposed();
            Wave++;
        }

        public void ApplyUpgrade(UpgradeType upgradeType)
        {
            ThrowIfDisposed();
            using (var item = new UpgradeItem(upgradeType))
            {
                item.ApplyTo(Player, UserData);
            }

            IsUpgradePending = false;
            CurrentUpgradeOptions = Array.Empty<UpgradeOption>();
        }

        public void Restart()
        {
            ThrowIfDisposed();
            DisposeEnemies();

            Wave = 1;
            Score = 0;
            IsUpgradePending = false;
            CurrentUpgradeOptions = Array.Empty<UpgradeOption>();
            Player.ResetForNewRun();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeEnemies();
            Player.Dispose();
            UserData.Dispose();
            isDisposed = true;
        }

        private UpgradeOption[] RollUpgradeOptions()
        {
            var pool = new[]
            {
                new UpgradeOption(UpgradeType.RapidShot, "Rapid Shot", "Shorter firing cooldown."),
                new UpgradeOption(UpgradeType.PowerShot, "Power Shot", "Arrows hit harder."),
                new UpgradeOption(UpgradeType.SwiftBoots, "Swift Boots", "Move faster."),
                new UpgradeOption(UpgradeType.Vitality, "Vitality", "Gain max health and heal.")
            };

            for (var i = pool.Length - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }

            return new[] { pool[0], pool[1], pool[2] };
        }

        private void DisposeEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                enemy.Dispose();
            }

            activeEnemies.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(ArcherGameSession));
            }
        }
    }
}
