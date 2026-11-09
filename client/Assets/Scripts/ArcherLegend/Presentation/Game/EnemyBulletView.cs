using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// 射撃する敵が撃つ弾です。プレイヤーに当たったらダメージ量を外へ渡し、ダメージ処理（無敵時間など）は呼び出し側に任せます。
    /// </summary>
    public sealed class EnemyBulletView : MonoBehaviour
    {
        /// <summary>弾の速さです。プレイヤーの移動速度（5.4）より少し速く、見てから横に避けられる程度にしています。</summary>
        public const float Speed = 6.5f;

        // プレイヤーの太さと弾の大きさを合わせた当たり判定の半径
        private const float HitRadius = 0.5f;
        private const float Lifetime = 4f;
        private const float ObstaclePadding = 0.12f;

        private Vector3 direction;
        private int damage;
        private float life;
        private Transform target;
        private Action<int> onHitTarget;
        private float arenaHalfWidth;
        private float arenaHalfDepth;
        private IReadOnlyList<Rect> obstacles;

        /// <param name="stageObstacles">ステージの障害物です。弾は障害物に当たると消えるので、障害物の陰に隠れれば防げます。</param>
        public void Launch(Vector3 start, Vector3 aimDirection, int shotDamage, Transform player, Action<int> hitPlayer, float halfWidth, float halfDepth, IReadOnlyList<Rect> stageObstacles)
        {
            obstacles = stageObstacles;
            transform.position = start;
            direction = aimDirection.normalized;
            damage = shotDamage;
            life = Lifetime;
            target = player;
            onHitTarget = hitPlayer;
            arenaHalfWidth = halfWidth;
            arenaHalfDepth = halfDepth;
        }

        private void Update()
        {
            var previous = transform.position;
            var next = previous + direction * Speed * Time.deltaTime;
            transform.position = next;
            life -= Time.deltaTime;

            // 矢と同じく 1 フレームで進む線分で調べ、低いフレームレートでもすり抜けないようにする
            if (target != null && ArrowView.IsHit(previous, next, target.position, HitRadius))
            {
                onHitTarget?.Invoke(damage);
                Destroy(gameObject);
                return;
            }

            var hitsObstacle = obstacles != null && ArenaGeometry.FindHit(previous, next, obstacles, ObstaclePadding) >= 0;
            if (hitsObstacle || life <= 0f || Mathf.Abs(next.x) > arenaHalfWidth || Mathf.Abs(next.z) > arenaHalfDepth)
            {
                Destroy(gameObject);
            }
        }
    }
}
