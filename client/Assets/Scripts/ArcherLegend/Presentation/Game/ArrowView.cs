using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// 矢の見た目用の橋渡し役です。ダメージ処理は命中時のコールバック経由で外へ委譲します。
    /// </summary>
    public sealed class ArrowView : MonoBehaviour
    {
        private const float Radius = 0.38f;

        private int damage;
        private float speed;
        private float life;
        private Vector3 direction;
        private IReadOnlyList<EnemyView> enemies;
        private System.Action<EnemyView> onEnemyHit;

        /// <param name="targets">生きている敵の一覧。倒された敵はすぐに外されるので、同じフレームで二重に命中しません。</param>
        public void Launch(Vector3 start, Vector3 aimDirection, int arrowDamage, float arrowSpeed, IReadOnlyList<EnemyView> targets, System.Action<EnemyView> enemyHit)
        {
            transform.position = start;
            direction = aimDirection.normalized;
            damage = arrowDamage;
            speed = arrowSpeed;
            life = 2.2f;
            enemies = targets;
            onEnemyHit = enemyHit;
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }

        /// <summary>
        /// 1 フレームで進む線分が、敵の円に触れたかを地面上（XZ 平面）で判定します。
        /// 線分で調べることで、フレームレートが低くても矢が敵をすり抜けません。
        /// </summary>
        public static bool IsHit(Vector3 from, Vector3 to, Vector3 center, float radius)
        {
            var start = new Vector2(from.x, from.z);
            var segment = new Vector2(to.x, to.z) - start;
            var toCenter = new Vector2(center.x, center.z) - start;
            var lengthSquared = segment.sqrMagnitude;
            var t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(toCenter, segment) / lengthSquared) : 0f;
            return (toCenter - segment * t).sqrMagnitude <= radius * radius;
        }

        private void Update()
        {
            var previous = transform.position;
            transform.position += direction * speed * Time.deltaTime;
            life -= Time.deltaTime;

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !IsHit(previous, transform.position, enemy.transform.position, Radius))
                {
                    continue;
                }

                if (enemy.ApplyDamage(damage))
                {
                    onEnemyHit?.Invoke(enemy);
                }

                Destroy(gameObject);
                return;
            }

            if (life <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
