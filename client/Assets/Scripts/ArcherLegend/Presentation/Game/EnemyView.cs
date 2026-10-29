using ArcherLegend.Domain;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    public sealed class EnemyView : MonoBehaviour
    {
        public Enemy Model { get; private set; }

        private const float ContactDistance = 1.05f;

        private Transform target;

        public void Construct(Enemy enemy, Transform player)
        {
            Model = enemy;
            target = player;
        }

        public bool ApplyDamage(int damage)
        {
            Model.TakeDamage(damage);
            if (Model.IsAlive)
            {
                return false;
            }

            Destroy(gameObject);
            return true;
        }

        public bool IsTouchingTarget()
        {
            return target != null && Vector3.Distance(transform.position, target.position) <= ContactDistance;
        }

        private void Update()
        {
            if (target == null || Model == null || !Model.IsAlive)
            {
                return;
            }

            var direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            transform.position += direction.normalized * Model.MoveSpeed * Time.deltaTime;
            transform.forward = direction.normalized;
        }
    }
}
