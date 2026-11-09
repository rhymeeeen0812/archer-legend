using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// ステージの障害物との当たり判定です。障害物は地面上（XZ 平面）の長方形で、Rect の x が X 座標、y が Z 座標です。
    /// 物理エンジンは使わず、座標の計算だけで判定するので、Unity を動かさずにテストできます。
    /// </summary>
    public static class ArenaGeometry
    {
        /// <summary>
        /// 円（キャラクター）が障害物に重なっていたら、重ならない位置まで押し出します。
        /// 障害物に沿って押し出すので、斜めにぶつかると壁沿いに滑るように動きます。
        /// </summary>
        public static Vector3 PushOut(Vector3 position, float radius, IReadOnlyList<Rect> obstacles)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                var rect = obstacles[i];
                var closestX = Mathf.Clamp(position.x, rect.xMin, rect.xMax);
                var closestZ = Mathf.Clamp(position.z, rect.yMin, rect.yMax);
                var dx = position.x - closestX;
                var dz = position.z - closestZ;
                var distanceSquared = dx * dx + dz * dz;
                if (distanceSquared >= radius * radius)
                {
                    continue;
                }

                if (distanceSquared > 0.000001f)
                {
                    var distance = Mathf.Sqrt(distanceSquared);
                    position.x = closestX + dx / distance * radius;
                    position.z = closestZ + dz / distance * radius;
                    continue;
                }

                // 中心が障害物の中に入ってしまったときは、いちばん近い辺から外へ出す
                var toLeft = position.x - rect.xMin;
                var toRight = rect.xMax - position.x;
                var toBottom = position.z - rect.yMin;
                var toTop = rect.yMax - position.z;
                var nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
                if (nearest == toLeft)
                {
                    position.x = rect.xMin - radius;
                }
                else if (nearest == toRight)
                {
                    position.x = rect.xMax + radius;
                }
                else if (nearest == toBottom)
                {
                    position.z = rect.yMin - radius;
                }
                else
                {
                    position.z = rect.yMax + radius;
                }
            }

            return position;
        }

        /// <summary>1 フレームで進む線分が、<paramref name="padding"/> だけ広げた障害物に触れたかを返します。</summary>
        public static bool SegmentHits(Vector3 from, Vector3 to, Rect rect, float padding)
        {
            // Liang–Barsky 法: 線分を長方形の 4 辺で順に切り詰め、残る区間があれば交わっている
            var t0 = 0f;
            var t1 = 1f;
            var dx = to.x - from.x;
            var dz = to.z - from.z;

            return Clip(-dx, from.x - (rect.xMin - padding), ref t0, ref t1)
                && Clip(dx, (rect.xMax + padding) - from.x, ref t0, ref t1)
                && Clip(-dz, from.z - (rect.yMin - padding), ref t0, ref t1)
                && Clip(dz, (rect.yMax + padding) - from.z, ref t0, ref t1);
        }

        /// <summary>線分が最初に触れた障害物の番号を返します。どれにも触れなければ -1 です。</summary>
        public static int FindHit(Vector3 from, Vector3 to, IReadOnlyList<Rect> obstacles, float padding)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                if (SegmentHits(from, to, obstacles[i], padding))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 障害物で跳ね返った向きを返します。ぶつかる前の位置が障害物の左右にあれば X を、上下にあれば Z を反転します（角なら両方）。
        /// </summary>
        public static Vector3 ReflectOff(Rect rect, Vector3 from, Vector3 direction)
        {
            var besideX = from.x < rect.xMin || from.x > rect.xMax;
            var besideZ = from.z < rect.yMin || from.z > rect.yMax;

            if (besideX)
            {
                direction.x = -direction.x;
            }

            if (besideZ || !besideX)
            {
                direction.z = -direction.z;
            }

            return direction;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f))
            {
                // 辺と平行なら、内側にあるときだけ交わる可能性が残る
                return q >= 0f;
            }

            var r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                {
                    return false;
                }

                t0 = Mathf.Max(t0, r);
            }
            else
            {
                if (r < t0)
                {
                    return false;
                }

                t1 = Mathf.Min(t1, r);
            }

            return true;
        }
    }
}
