using System;
using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// プレイヤー固有のルールです。武器・スキル・装備の拡張を Unity の振る舞いクラスから切り離します。
    /// </summary>
    public sealed class Player : CombatantBase
    {
        /// <summary>被弾してから次のダメージを受け付けるまでの無敵時間（秒）です。</summary>
        public const float InvincibleDuration = 1.2f;

        /// <summary>一度に放つ矢の最大数です。拡散矢を 1 回取るごとに左右へ 1 本ずつ増えます。</summary>
        public const int MaxArrowCount = 5;

        /// <summary>1 本の矢が貫通できる敵の最大数です。</summary>
        public const int MaxPierceCount = 3;

        /// <summary>1 本の矢が壁で跳ね返る最大回数です。</summary>
        public const int MaxBounceCount = 3;

        public Player() : base("player", 100)
        {
        }

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNext { get; private set; } = 6;
        public float MoveSpeed { get; private set; } = 5.4f;
        public float FireCooldown { get; private set; } = 0.48f;
        public int ArrowDamage { get; private set; } = 28;
        public int ArrowCount { get; private set; } = 1;
        public int PierceCount { get; private set; }
        public int BounceCount { get; private set; }
        public float InvincibleTimeRemaining { get; private set; }
        public bool IsInvincible => InvincibleTimeRemaining > 0f;

        /// <summary>
        /// 無敵時間中のダメージは無視します。ダメージを受けたら無敵時間を開始し、受けたかどうかを返します。
        /// </summary>
        public bool TryTakeHit(int amount)
        {
            ThrowIfDisposed();
            if (IsInvincible || !IsAlive)
            {
                return false;
            }

            TakeDamage(amount);
            InvincibleTimeRemaining = InvincibleDuration;
            return true;
        }

        public void TickInvincibility(float deltaTime)
        {
            ThrowIfDisposed();
            InvincibleTimeRemaining = Mathf.Max(0f, InvincibleTimeRemaining - Mathf.Max(0f, deltaTime));
        }

        /// <summary>経験値を加え、上がったレベルの数を返します。1 回で複数のレベルが上がることもあります。</summary>
        public int AddExperience(int amount)
        {
            ThrowIfDisposed();
            Experience += Mathf.Max(0, amount);

            var levelsGained = 0;
            while (Experience >= ExperienceToNext)
            {
                Experience -= ExperienceToNext;
                Level++;
                ExperienceToNext = Mathf.RoundToInt(ExperienceToNext * 1.45f + 3f);
                levelsGained++;
            }

            return levelsGained;
        }

        /// <summary>まだ強化できるかを返します。上限に達した強化は選択肢に出しません。</summary>
        public bool CanUpgrade(UpgradeType upgradeType)
        {
            ThrowIfDisposed();
            switch (upgradeType)
            {
                case UpgradeType.SpreadShot:
                    return ArrowCount < MaxArrowCount;
                case UpgradeType.PiercingShot:
                    return PierceCount < MaxPierceCount;
                case UpgradeType.BouncingShot:
                    return BounceCount < MaxBounceCount;
                default:
                    return true;
            }
        }

        public void ApplyUpgrade(UpgradeType upgradeType)
        {
            ThrowIfDisposed();
            switch (upgradeType)
            {
                case UpgradeType.RapidShot:
                    FireCooldown = Mathf.Max(0.16f, FireCooldown * 0.82f);
                    break;
                case UpgradeType.PowerShot:
                    ArrowDamage += 10;
                    break;
                case UpgradeType.SwiftBoots:
                    MoveSpeed += 0.55f;
                    break;
                case UpgradeType.Vitality:
                    MaxHealth += 25;
                    Heal(35);
                    break;
                case UpgradeType.SpreadShot:
                    // 狙った方向を中心に左右対称に広げるので、2 本ずつ増やして本数を奇数に保つ
                    ArrowCount = Mathf.Min(MaxArrowCount, ArrowCount + 2);
                    break;
                case UpgradeType.PiercingShot:
                    PierceCount = Mathf.Min(MaxPierceCount, PierceCount + 1);
                    break;
                case UpgradeType.BouncingShot:
                    BounceCount = Mathf.Min(MaxBounceCount, BounceCount + 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null);
            }
        }

        public void ResetForNewRun()
        {
            ThrowIfDisposed();
            Level = 1;
            Experience = 0;
            ExperienceToNext = 6;
            MaxHealth = 100;
            MoveSpeed = 5.4f;
            FireCooldown = 0.48f;
            ArrowDamage = 28;
            ArrowCount = 1;
            PierceCount = 0;
            BounceCount = 0;
            InvincibleTimeRemaining = 0f;
            HealToFull();
        }
    }
}
