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

        public Player() : base("player", 100)
        {
        }

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNext { get; private set; } = 6;
        public float MoveSpeed { get; private set; } = 5.4f;
        public float FireCooldown { get; private set; } = 0.48f;
        public int ArrowDamage { get; private set; } = 28;
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

        public bool AddExperience(int amount)
        {
            ThrowIfDisposed();
            Experience += Mathf.Max(0, amount);
            if (Experience < ExperienceToNext)
            {
                return false;
            }

            Experience -= ExperienceToNext;
            Level++;
            ExperienceToNext = Mathf.RoundToInt(ExperienceToNext * 1.45f + 3f);
            return true;
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
            InvincibleTimeRemaining = 0f;
            HealToFull();
        }
    }
}
