using System;
using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// プレイヤー固有のルールです。武器・スキル・装備の拡張を Unity の振る舞いクラスから切り離します。
    /// </summary>
    public sealed class Player : CombatantBase
    {
        public Player() : base("player", 100)
        {
        }

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNext { get; private set; } = 6;
        public float MoveSpeed { get; private set; } = 5.4f;
        public float FireCooldown { get; private set; } = 0.48f;
        public int ArrowDamage { get; private set; } = 28;

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
            HealToFull();
        }
    }
}
