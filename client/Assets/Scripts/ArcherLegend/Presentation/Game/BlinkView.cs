using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// 見た目を点滅させます。無敵時間かどうかの判断は持たず、呼び出し側から点滅のオン・オフだけを受け取ります。
    /// </summary>
    public sealed class BlinkView : MonoBehaviour
    {
        private const float BlinkInterval = 0.08f;

        private Renderer[] renderers;
        private bool isBlinking;
        private float elapsed;

        public void SetBlinking(bool blinking)
        {
            if (isBlinking == blinking)
            {
                return;
            }

            isBlinking = blinking;
            elapsed = 0f;
            SetVisible(true);
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if (!isBlinking)
            {
                return;
            }

            // Time.deltaTime で進めるので、レベルアップ選択中など時間が止まっている間は点滅も止まる
            elapsed += Time.deltaTime;
            SetVisible(Mathf.FloorToInt(elapsed / BlinkInterval) % 2 == 1);
        }

        private void SetVisible(bool visible)
        {
            foreach (var target in renderers)
            {
                target.enabled = visible;
            }
        }
    }
}
