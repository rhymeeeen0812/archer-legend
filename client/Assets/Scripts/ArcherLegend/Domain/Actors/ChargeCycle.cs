using UnityEngine;

namespace ArcherLegend.Domain
{
    public enum ChargePhase
    {
        /// <summary>ゆっくり近づく</summary>
        Approach,

        /// <summary>立ち止まって突進の向きを決める（プレイヤーが避けるための予兆）</summary>
        Windup,

        /// <summary>決めた向きへ一直線に突進する</summary>
        Dash,

        /// <summary>突進のあと、しばらく次の突進をしない</summary>
        Recover
    }

    /// <summary>
    /// 突進する敵の行動の流れです。接近 → 溜め → 突進 → 休み → 接近 … を繰り返します。
    /// Unity の Transform を持たないので、時間と距離だけで状態の移り変わりを確かめられます。
    /// </summary>
    public sealed class ChargeCycle
    {
        /// <summary>この距離まで近づいたら溜めに入ります。</summary>
        public const float TriggerDistance = 5.5f;

        public const float WindupDuration = 0.7f;
        public const float DashDuration = 0.45f;
        public const float RecoverDuration = 1.5f;

        /// <summary>突進中の速さです。DashDuration と掛けると、1 回の突進で進む距離（約 4.5）になります。</summary>
        public const float DashSpeed = 10f;

        private float remaining;

        public ChargePhase Phase { get; private set; } = ChargePhase.Approach;

        /// <summary>時間を進め、進めたあとの状態を返します。</summary>
        public ChargePhase Tick(float deltaTime, float distanceToTarget)
        {
            if (Phase == ChargePhase.Approach)
            {
                if (distanceToTarget <= TriggerDistance)
                {
                    Enter(ChargePhase.Windup, WindupDuration);
                }

                return Phase;
            }

            remaining -= Mathf.Max(0f, deltaTime);
            if (remaining > 0f)
            {
                return Phase;
            }

            switch (Phase)
            {
                case ChargePhase.Windup:
                    Enter(ChargePhase.Dash, DashDuration);
                    break;
                case ChargePhase.Dash:
                    Enter(ChargePhase.Recover, RecoverDuration);
                    break;
                default:
                    Phase = ChargePhase.Approach;
                    break;
            }

            return Phase;
        }

        private void Enter(ChargePhase phase, float duration)
        {
            Phase = phase;
            remaining = duration;
        }
    }
}
