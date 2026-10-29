using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// コードから uGUI を組み立てるための共通処理です。各 View で同じ生成コードを重複させないために使います。
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color PrimaryButtonColor = new Color(0.12f, 0.46f, 0.38f, 0.95f);
        public static readonly Color SecondaryButtonColor = new Color(0.08f, 0.10f, 0.12f, 0.92f);
        public static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.72f);
        public static readonly Color HighlightColor = new Color(1f, 0.84f, 0.29f);

        public static void SetupCanvas(GameObject target, int sortingOrder = 0)
        {
            var canvas = target.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            target.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            target.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// シーンに置かれたカメラを止め、単色背景のカメラを作ります。メニュー系の画面で使います。
        /// </summary>
        public static void CreateMenuCamera()
        {
            foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existingCamera.enabled = false;
            }

            var camera = new GameObject("Menu Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.12f);
        }

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color color, int fontSize)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(buttonObject.transform, "Label", fontSize);
            text.text = label;
            Stretch(text.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return button;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);

            var image = panelObject.AddComponent<Image>();
            image.color = color;
            Stretch(image.rectTransform, Vector2.zero, Vector2.zero);
            return image;
        }

        public static void PlaceCenter(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        public static void PlaceTopLeft(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        public static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
