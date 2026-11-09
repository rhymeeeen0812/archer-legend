using System;
using ArcherLegend.Domain;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// 敵の動きの見た目です。種類ごとの性能は <see cref="Enemy"/>、突進の流れは <see cref="ChargeCycle"/> に任せ、
    /// ここでは Transform を動かすことと、弾を撃つタイミングを外へ知らせることだけを行います。
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        /// <summary>射撃する敵が保とうとするプレイヤーとの距離です。</summary>
        public const float PreferredShootDistance = 5.5f;

        private const float ContactDistance = 1.05f;
        private const float ShootDistanceTolerance = 0.6f;
        private const float ArenaEdgeMargin = 0.6f;

        private Transform target;
        private float arenaHalfWidth;
        private float arenaHalfDepth;
        private Action<EnemyView, Vector3> onShoot;
        private float shotTimer;
        private ChargeCycle charge;
        private ChargePhase lastChargePhase;
        private Vector3 dashDirection;
        private BlinkView blink;

        public Enemy Model { get; private set; }

        /// <param name="shoot">射撃する敵が弾を撃つときに、撃った敵と向きを渡して呼ばれます。弾の生成は呼び出し側が行います。</param>
        public void Construct(Enemy enemy, Transform player, float halfWidth, float halfDepth, Action<EnemyView, Vector3> shoot)
        {
            Model = enemy;
            target = player;
            arenaHalfWidth = halfWidth;
            arenaHalfDepth = halfDepth;
            onShoot = shoot;

            // 出現した瞬間に全員が一斉に撃たないよう、最初の 1 発までの時間をばらつかせる
            shotTimer = enemy.ShotInterval * UnityEngine.Random.Range(0.5f, 1.0f);

            if (enemy.Type == EnemyType.Charger)
            {
                charge = new ChargeCycle();
                lastChargePhase = charge.Phase;

                // 溜めている間は点滅させて、突進が来ることをプレイヤーに知らせる
                blink = gameObject.AddComponent<BlinkView>();
            }
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

        /// <summary>
        /// 射撃する敵の移動の向きを返します。遠すぎれば近づき（1）、近すぎれば離れ（-1）、ちょうどよければ止まります（0）。
        /// </summary>
        public static int ShooterMoveSign(float distance, float preferredDistance, float tolerance)
        {
            if (distance > preferredDistance + tolerance)
            {
                return 1;
            }

            return distance < preferredDistance - tolerance ? -1 : 0;
        }

        private void Update()
        {
            if (target == null || Model == null || !Model.IsAlive)
            {
                return;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            var facing = distance > 0.01f ? toTarget / distance : transform.forward;

            switch (Model.Type)
            {
                case EnemyType.Shooter:
                    UpdateShooter(facing, distance);
                    break;
                case EnemyType.Charger:
                    UpdateCharger(facing, distance);
                    break;
                default:
                    if (distance > 0.1f)
                    {
                        Move(facing, Model.MoveSpeed);
                    }

                    break;
            }
        }

        private void UpdateShooter(Vector3 facing, float distance)
        {
            var sign = ShooterMoveSign(distance, PreferredShootDistance, ShootDistanceTolerance);
            if (sign != 0)
            {
                Move(facing * sign, Model.MoveSpeed);
            }

            // 下がっているときもプレイヤーの方を向いたまま撃つ
            transform.forward = facing;

            shotTimer -= Time.deltaTime;
            if (shotTimer > 0f)
            {
                return;
            }

            shotTimer = Model.ShotInterval;
            onShoot?.Invoke(this, facing);
        }

        private void UpdateCharger(Vector3 facing, float distance)
        {
            var phase = charge.Tick(Time.deltaTime, distance);

            // 突進の向きは溜めに入った瞬間に決める。溜めの間に横へ動けば避けられる
            if (phase == ChargePhase.Windup && lastChargePhase != ChargePhase.Windup)
            {
                dashDirection = facing;
                transform.forward = facing;
            }

            lastChargePhase = phase;
            blink.SetBlinking(phase == ChargePhase.Windup);

            switch (phase)
            {
                case ChargePhase.Approach:
                    Move(facing, Model.MoveSpeed);
                    break;
                case ChargePhase.Dash:
                    Move(dashDirection, ChargeCycle.DashSpeed);
                    break;
                case ChargePhase.Recover:
                    Move(facing, Model.MoveSpeed * 0.5f);
                    break;
            }
        }

        private void Move(Vector3 direction, float speed)
        {
            var next = transform.position + direction * speed * Time.deltaTime;
            transform.position = new Vector3(
                Mathf.Clamp(next.x, -arenaHalfWidth + ArenaEdgeMargin, arenaHalfWidth - ArenaEdgeMargin),
                next.y,
                Mathf.Clamp(next.z, -arenaHalfDepth + ArenaEdgeMargin, arenaHalfDepth - ArenaEdgeMargin));

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.forward = direction.normalized;
            }
        }
    }
}
