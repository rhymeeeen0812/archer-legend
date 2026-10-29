using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// 敵のドメインモデルです。移動や当たり判定の見た目は表示側のクラスに任せます。
    /// </summary>
    public sealed class Enemy : CombatantBase
    {
        public Enemy(int wave) : base($"enemy-wave-{wave}-{System.Guid.NewGuid():N}", 45 + wave * 10)
        {
            ContactDamage = 10 + wave;
            ExperienceReward = 2 + wave;
            MoveSpeed = 2.0f + Mathf.Min(1.8f, wave * 0.13f);
        }

        public int ContactDamage { get; }
        public int ExperienceReward { get; }
        public float MoveSpeed { get; }
    }
}
