using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// 1 本の矢の性能です。プレイヤーの強化状態から、放つときに決めます。
    /// </summary>
    public readonly struct ArrowShot
    {
        public ArrowShot(int damage, float speed, int pierceCount, int bounceCount, float arenaHalfWidth, float arenaHalfDepth)
        {
            Damage = damage;
            Speed = speed;
            PierceCount = pierceCount;
            BounceCount = bounceCount;
            ArenaHalfWidth = arenaHalfWidth;
            ArenaHalfDepth = arenaHalfDepth;
        }

        public int Damage { get; }
        public float Speed { get; }

        /// <summary>敵に当たっても消えずに進める回数です。</summary>
        public int PierceCount { get; }

        /// <summary>壁で跳ね返れる回数です。</summary>
        public int BounceCount { get; }

        public float ArenaHalfWidth { get; }
        public float ArenaHalfDepth { get; }
    }

    /// <summary>
    /// 矢の見た目用の橋渡し役です。ダメージ処理は命中時のコールバック経由で外へ委譲します。
    /// </summary>
    public sealed class ArrowView : MonoBehaviour
    {
        private const float Radius = 0.38f;

        // 貫通した矢が、同じ敵に毎フレーム当たり続けないように覚えておく
        private readonly HashSet<EnemyView> hitEnemies = new HashSet<EnemyView>();

        private ArrowShot shot;
        private int remainingPierces;
        private int remainingBounces;
        private float life;
        private Vector3 direction;
        private IReadOnlyList<EnemyView> enemies;
        private System.Action<EnemyView> onEnemyHit;

        /// <param name="targets">生きている敵の一覧。倒された敵はすぐに外されるので、同じフレームで二重に命中しません。</param>
        public void Launch(Vector3 start, Vector3 aimDirection, ArrowShot arrowShot, IReadOnlyList<EnemyView> targets, System.Action<EnemyView> enemyHit)
        {
            transform.position = start;
            shot = arrowShot;
            remainingPierces = arrowShot.PierceCount;
            remainingBounces = arrowShot.BounceCount;
            life = 2.2f;
            enemies = targets;
            onEnemyHit = enemyHit;
            SetDirection(aimDirection);
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

        /// <summary>
        /// 狙った方向を中心に、<paramref name="angleStep"/> 度ずつ左右対称に広げた向きを返します。1 本なら狙った方向そのままです。
        /// </summary>
        public static Vector3[] SpreadDirections(Vector3 aim, int count, float angleStep)
        {
            aim.y = 0f;
            aim.Normalize();

            var directions = new Vector3[Mathf.Max(1, count)];
            var firstAngle = -(directions.Length - 1) * 0.5f * angleStep;
            for (var i = 0; i < directions.Length; i++)
            {
                directions[i] = Quaternion.AngleAxis(firstAngle + i * angleStep, Vector3.up) * aim;
            }

            return directions;
        }

        /// <summary>
        /// アリーナの壁を越えていたら、壁で折り返した位置と向きを返します。
        /// 壁から離れる向きに進んでいるときは跳ね返しません（跳ね返った直後に再び折り返さないため）。
        /// </summary>
        public static bool TryBounce(Vector3 position, Vector3 direction, float halfWidth, float halfDepth, out Vector3 bouncedPosition, out Vector3 bouncedDirection)
        {
            bouncedPosition = position;
            bouncedDirection = direction;
            var bounced = false;

            if (Mathf.Abs(position.x) > halfWidth && position.x * direction.x > 0f)
            {
                bouncedPosition.x = Mathf.Sign(position.x) * halfWidth * 2f - position.x;
                bouncedDirection.x = -direction.x;
                bounced = true;
            }

            if (Mathf.Abs(position.z) > halfDepth && position.z * direction.z > 0f)
            {
                bouncedPosition.z = Mathf.Sign(position.z) * halfDepth * 2f - position.z;
                bouncedDirection.z = -direction.z;
                bounced = true;
            }

            return bounced;
        }

        private void Update()
        {
            var previous = transform.position;
            var next = previous + direction * shot.Speed * Time.deltaTime;
            life -= Time.deltaTime;

            if (remainingBounces > 0 && TryBounce(next, direction, shot.ArenaHalfWidth, shot.ArenaHalfDepth, out var bouncedPosition, out var bouncedDirection))
            {
                // 跳ね返ったフレームの当たり判定は、壁で折れた経路を始点と終点の直線で近似する（1 フレームの移動量は小さいので十分）
                next = bouncedPosition;
                SetDirection(bouncedDirection);
                remainingBounces--;

                // 跳ね返ったあとは、一度当たった敵にもまた当たれる
                hitEnemies.Clear();
            }

            transform.position = next;

            // 命中して倒した敵は一覧から外されるので、後ろから順に調べる
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null || hitEnemies.Contains(enemy) || !IsHit(previous, next, enemy.transform.position, Radius))
                {
                    continue;
                }

                hitEnemies.Add(enemy);
                if (enemy.ApplyDamage(shot.Damage))
                {
                    onEnemyHit?.Invoke(enemy);
                }

                if (remainingPierces <= 0)
                {
                    Destroy(gameObject);
                    return;
                }

                remainingPierces--;
            }

            if (life <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void SetDirection(Vector3 newDirection)
        {
            direction = newDirection.normalized;
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
