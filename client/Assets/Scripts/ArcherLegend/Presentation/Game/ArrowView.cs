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
        private System.Action<EnemyView> onEnemyHit;

        public void Launch(Vector3 start, Vector3 aimDirection, int arrowDamage, float arrowSpeed, System.Action<EnemyView> enemyHit)
        {
            transform.position = start;
            direction = aimDirection.normalized;
            damage = arrowDamage;
            speed = arrowSpeed;
            life = 2.2f;
            onEnemyHit = enemyHit;
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
            life -= Time.deltaTime;

            var enemies = FindObjectsByType<EnemyView>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy == null || Vector3.Distance(transform.position, enemy.transform.position) > Radius)
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
