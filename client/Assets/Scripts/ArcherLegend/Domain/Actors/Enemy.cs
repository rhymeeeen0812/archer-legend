using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// 敵のドメインモデルです。移動や当たり判定の見た目は表示側のクラスに任せます。
    /// 種類ごとの性能はここで決め、ウェーブが進むほど強くなります。
    /// </summary>
    public sealed class Enemy : CombatantBase
    {
        public Enemy(int wave, EnemyType type = EnemyType.Chaser) : base($"enemy-{type}-wave-{wave}-{System.Guid.NewGuid():N}", MaxHealthFor(wave, type))
        {
            Type = type;
            switch (type)
            {
                case EnemyType.Shooter:
                    // 打たれ弱いが、離れた位置から弾を撃ってくる
                    ContactDamage = 8 + wave;
                    ExperienceReward = 3 + wave;
                    MoveSpeed = 1.6f + Mathf.Min(0.8f, wave * 0.06f);
                    ShotDamage = 8 + wave;
                    ShotInterval = Mathf.Max(1.4f, 2.6f - wave * 0.08f);
                    break;
                case EnemyType.Charger:
                    // 普段はゆっくり歩き、溜めてから一直線に突進する
                    ContactDamage = 14 + wave;
                    ExperienceReward = 4 + wave;
                    MoveSpeed = 1.3f + Mathf.Min(0.6f, wave * 0.05f);
                    break;
                default:
                    ContactDamage = 10 + wave;
                    ExperienceReward = 2 + wave;
                    MoveSpeed = 2.0f + Mathf.Min(1.8f, wave * 0.13f);
                    break;
            }
        }

        public EnemyType Type { get; }
        public int ContactDamage { get; }
        public int ExperienceReward { get; }
        public float MoveSpeed { get; }

        /// <summary>射撃する敵の弾のダメージです。射撃しない敵は 0 です。</summary>
        public int ShotDamage { get; }

        /// <summary>射撃する敵が弾を撃つ間隔（秒）です。射撃しない敵は 0 です。</summary>
        public float ShotInterval { get; }

        private static int MaxHealthFor(int wave, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Shooter:
                    return 30 + wave * 8;
                case EnemyType.Charger:
                    return 60 + wave * 12;
                default:
                    return 45 + wave * 10;
            }
        }
    }
}
