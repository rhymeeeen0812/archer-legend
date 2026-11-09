using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// ウェーブごとの敵の顔ぶれを決めます。序盤は追跡する敵だけにして、ウェーブが進むと射撃・突進する敵が混ざります。
    /// </summary>
    public static class EnemyWave
    {
        /// <summary>射撃する敵が出始めるウェーブです。</summary>
        public const int FirstShooterWave = 3;

        /// <summary>突進する敵が出始めるウェーブです。</summary>
        public const int FirstChargerWave = 5;

        public static int EnemyCount(int wave) => 3 + Mathf.Max(1, wave);

        /// <summary>
        /// そのウェーブに出す敵の種類を、出現順に返します。
        /// 同じ種類が固まって出ないよう、種類を順番に混ぜて並べます。
        /// </summary>
        public static EnemyType[] Compose(int wave)
        {
            var total = EnemyCount(wave);

            // 追跡する敵が常に 3 分の 1 以上残るよう、ほかの種類の数には上限を付ける（射撃は 3 分の 1、突進は 4 分の 1 まで）
            var shooters = wave >= FirstShooterWave ? Mathf.Min((wave - 1) / 2, total / 3) : 0;
            var chargers = wave >= FirstChargerWave ? Mathf.Min((wave - 3) / 2, total / 4) : 0;
            var chasers = total - shooters - chargers;

            var result = new List<EnemyType>(total);
            while (result.Count < total)
            {
                if (chasers-- > 0)
                {
                    result.Add(EnemyType.Chaser);
                }

                if (shooters-- > 0)
                {
                    result.Add(EnemyType.Shooter);
                }

                if (chargers-- > 0)
                {
                    result.Add(EnemyType.Charger);
                }
            }

            return result.ToArray();
        }
    }
}
