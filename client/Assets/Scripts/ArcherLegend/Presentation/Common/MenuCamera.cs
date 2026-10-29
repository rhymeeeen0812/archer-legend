using UnityEngine;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// メニュー系の画面で使う、単色背景のカメラです。
    /// </summary>
    public static class MenuCamera
    {
        private static readonly Color BackgroundColor = new Color(0.09f, 0.10f, 0.12f);

        /// <summary>
        /// シーンに置かれたカメラを止め、単色背景のカメラを作ります。
        /// </summary>
        public static void Create()
        {
            foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existingCamera.enabled = false;
            }

            var camera = new GameObject("Menu Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
        }
    }
}
