using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Scenes
{
    /// <summary>
    /// タイトル画面とホーム画面で使う簡易 UI です。将来プレハブ化するときは、このクラスだけ差し替えます。
    /// </summary>
    public sealed class MenuScenePresenter : MonoBehaviour
    {
        public void ShowTitle()
        {
            BuildMenu(
                "Archer Legend",
                "Press Start",
                "Start",
                () => SceneManager.LoadScene(SceneNames.Home));
        }

        public void ShowHome()
        {
            BuildMenu(
                "Home",
                "装備や強化を置くための拡張用シーンです。",
                "Adventure",
                () => SceneManager.LoadScene(SceneNames.Game));
        }

        private void BuildMenu(string title, string message, string buttonLabel, UnityEngine.Events.UnityAction onClick)
        {
            foreach (var existingCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existingCamera.enabled = false;
            }

            var cameraObject = new GameObject("Menu Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.12f);

            var canvasObject = new GameObject("Menu Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateText("Title", canvasObject.transform, title, new Vector2(0f, 110f), 46);
            CreateText("Message", canvasObject.transform, message, new Vector2(0f, 34f), 20);
            CreateButton("Primary Button", canvasObject.transform, buttonLabel, new Vector2(0f, -62f), onClick);
        }

        private static Text CreateText(string name, Transform parent, string value, Vector2 anchoredPosition, int fontSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;

            var rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(620f, 72f);
            return text;
        }

        private static void CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.46f, 0.38f, 0.95f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(240f, 54f);

            var text = CreateText("Label", buttonObject.transform, label, Vector2.zero, 22);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
