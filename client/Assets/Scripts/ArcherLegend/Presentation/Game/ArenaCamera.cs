using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// アリーナ全体が常に画面に収まるよう、画面の縦横比に合わせて正射影カメラの大きさを調整します。
    /// Game ビューのサイズや端末の向きが変わっても、端が見切れないようにするためです。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        private const float PitchAngle = 78f;
        private const float Distance = 20f;

        // アリーナの外側に確保する余白（ワールド単位）。HUD と重なっても端が隠れないようにする
        private const float Margin = 2f;

        // 横長の画面でもこれより近づけない
        private const float MinOrthographicSize = 9.5f;

        private Camera targetCamera;
        private float arenaHalfWidth;
        private float arenaHalfDepth;
        private float fittedAspect;

        public static ArenaCamera Create(float halfWidth, float halfDepth)
        {
            foreach (var existingCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existingCamera.enabled = false;
            }

            var camera = new GameObject("Arena Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.12f, 0.15f);

            // アリーナの中心を画面の中心に捉える
            camera.transform.rotation = Quaternion.Euler(PitchAngle, 0f, 0f);
            camera.transform.position = -camera.transform.forward * Distance;

            var arenaCamera = camera.gameObject.AddComponent<ArenaCamera>();
            arenaCamera.targetCamera = camera;
            arenaCamera.arenaHalfWidth = halfWidth;
            arenaCamera.arenaHalfDepth = halfDepth;
            arenaCamera.Fit();
            return arenaCamera;
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(targetCamera.aspect, fittedAspect))
            {
                Fit();
            }
        }

        private void Fit()
        {
            fittedAspect = targetCamera.aspect;

            // 斜めから見下ろしているので、奥行きは sin(角度) 倍に縮んで映る
            var halfHeightForDepth = (arenaHalfDepth + Margin) * Mathf.Sin(PitchAngle * Mathf.Deg2Rad);
            var halfHeightForWidth = (arenaHalfWidth + Margin) / fittedAspect;
            targetCamera.orthographicSize = Mathf.Max(MinOrthographicSize, halfHeightForDepth, halfHeightForWidth);
        }
    }
}
