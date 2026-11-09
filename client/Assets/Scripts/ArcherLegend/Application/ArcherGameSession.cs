using System;
using System.Collections.Generic;
using System.Linq;
using ArcherLegend.Domain;

namespace ArcherLegend.Application
{
    /// <summary>
    /// アプリケーション層のユースケースです。ランの生成・破棄を管理し、Domain の明示的な Dispose を担当します。
    /// </summary>
    public sealed class ArcherGameSession : IDisposable
    {
        private const int UpgradeChoiceCount = 3;

        private static readonly UpgradeOption[] AllUpgradeOptions =
        {
            new UpgradeOption(UpgradeType.RapidShot, "連射", "矢を放つ間隔が短くなる"),
            new UpgradeOption(UpgradeType.PowerShot, "強弓", "矢のダメージが上がる"),
            new UpgradeOption(UpgradeType.SwiftBoots, "疾風の靴", "移動が速くなる"),
            new UpgradeOption(UpgradeType.Vitality, "活力", "最大HPが増え、HPが回復する"),
            new UpgradeOption(UpgradeType.SpreadShot, "拡散矢", "矢が左右に 1 本ずつ増える"),
            new UpgradeOption(UpgradeType.PiercingShot, "貫通矢", "矢が敵を 1 体多く貫く"),
            new UpgradeOption(UpgradeType.BouncingShot, "跳弾", "矢が壁で 1 回多く跳ね返る")
        };

        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly System.Random random = new System.Random();
        private int pendingUpgrades;
        private bool isDisposed;

        public Player Player { get; } = new Player();
        /// <summary>通算のウェーブ数です。サーバーやセーブデータにはこの値を記録します。</summary>
        public int Wave { get; private set; } = 1;

        /// <summary>今いるステージ（部屋）の番号です。通算のウェーブ数から求めます。</summary>
        public int Stage => StageProgress.StageOf(Wave);

        /// <summary>今のステージの何番目のウェーブかを返します。</summary>
        public int WaveInStage => StageProgress.WaveInStage(Wave);

        /// <summary>今のウェーブを倒しきると、ステージの出口が開くかを返します。</summary>
        public bool IsLastWaveOfStage => StageProgress.IsLastWaveOfStage(Wave);
        public int Score { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public bool IsUpgradePending => pendingUpgrades > 0;
        public bool IsGameOver => !Player.IsAlive;

        public UpgradeOption[] CurrentUpgradeOptions { get; private set; } = Array.Empty<UpgradeOption>();

        /// <summary>今のウェーブに出す敵の種類を、出現順に返します。</summary>
        public EnemyType[] ComposeWave()
        {
            ThrowIfDisposed();
            return EnemyWave.Compose(Wave);
        }

        public Enemy CreateEnemy(EnemyType type = EnemyType.Chaser)
        {
            ThrowIfDisposed();
            var enemy = new Enemy(Wave, type);
            activeEnemies.Add(enemy);
            return enemy;
        }

        public void RegisterEnemyDefeated(Enemy enemy)
        {
            ThrowIfDisposed();
            Score += 10 * Wave;
            EnemiesDefeated++;

            // 選択中にレベルが上がっても、表示中の選択肢は変えずに残りの回数を増やす
            var wasPending = IsUpgradePending;
            pendingUpgrades += Player.AddExperience(enemy.ExperienceReward);
            if (!wasPending && IsUpgradePending)
            {
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
                item.ApplyTo(Player);
            }

            pendingUpgrades--;
            CurrentUpgradeOptions = IsUpgradePending ? RollUpgradeOptions() : Array.Empty<UpgradeOption>();
        }

        public RunResult CreateRunResult()
        {
            ThrowIfDisposed();
            return new RunResult(Score, Wave, EnemiesDefeated, Player.Level);
        }

        public void Restart()
        {
            ThrowIfDisposed();
            DisposeEnemies();

            Wave = 1;
            Score = 0;
            EnemiesDefeated = 0;
            pendingUpgrades = 0;
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
            isDisposed = true;
        }

        private UpgradeOption[] RollUpgradeOptions()
        {
            // 上限に達した強化は外す。数値が上がるだけの 4 種は上限がないので、選択肢は必ず 3 つ以上残る
            var pool = AllUpgradeOptions.Where(option => Player.CanUpgrade(option.Type)).ToArray();

            for (var i = pool.Length - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }

            return pool.Take(UpgradeChoiceCount).ToArray();
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
