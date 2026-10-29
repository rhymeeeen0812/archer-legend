using System;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// 画面表示の橋渡し役です。セッションの状態を読むだけにして、ドメインモデルを直接変更しません。
    /// </summary>
    public sealed class GameHudPresenter : MonoBehaviour
    {
        private Text statusText;
        private Text centerText;
        private Button[] upgradeButtons;

        public void Construct(Action<UpgradeType> upgradeSelected, Action restartSelected)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            statusText = CreateText("Status", transform, new Vector2(12f, -12f), TextAnchor.UpperLeft, 20);
            centerText = CreateText("Center Message", transform, Vector2.zero, TextAnchor.MiddleCenter, 28);
            centerText.rectTransform.anchorMin = new Vector2(0.16f, 0.34f);
            centerText.rectTransform.anchorMax = new Vector2(0.84f, 0.66f);
            centerText.rectTransform.offsetMin = Vector2.zero;
            centerText.rectTransform.offsetMax = Vector2.zero;

            upgradeButtons = new Button[3];
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var index = i;
                upgradeButtons[i] = CreateButton($"Upgrade {i + 1}", transform, new Vector2(0f, 80f - i * 58f));
                upgradeButtons[i].onClick.AddListener(() => upgradeSelected(GetButtonType(index)));
            }

            var restartButton = CreateButton("Restart", transform, new Vector2(0f, -160f));
            restartButton.GetComponentInChildren<Text>().text = "Restart";
            restartButton.onClick.AddListener(() => restartSelected());
            restartButton.gameObject.SetActive(false);
        }

        public void Render(ArcherGameSession session, int aliveEnemies)
        {
            var player = session.Player;
            statusText.text = $"HP {player.Health}/{player.MaxHealth}\nLV {player.Level}  XP {player.Experience}/{player.ExperienceToNext}\nWave {session.Wave}  Score {session.Score}\nEnemies {aliveEnemies}";

            if (session.IsGameOver)
            {
                centerText.text = "Game Over\nPress R to Restart";
                SetUpgradesVisible(false, Array.Empty<UpgradeOption>());
                return;
            }

            if (session.IsUpgradePending)
            {
                centerText.text = "Level Up";
                SetUpgradesVisible(true, session.CurrentUpgradeOptions);
                return;
            }

            centerText.text = string.Empty;
            SetUpgradesVisible(false, Array.Empty<UpgradeOption>());
        }

        private UpgradeType GetButtonType(int index)
        {
            return (UpgradeType)upgradeButtons[index].gameObject.GetComponent<UpgradeButtonState>().UpgradeType;
        }

        private void SetUpgradesVisible(bool visible, UpgradeOption[] options)
        {
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var button = upgradeButtons[i];
                button.gameObject.SetActive(visible);
                if (!visible || i >= options.Length)
                {
                    continue;
                }

                button.GetComponent<UpgradeButtonState>().UpgradeType = options[i].Type;
                button.GetComponentInChildren<Text>().text = $"{options[i].Title}\n{options[i].Description}";
            }
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, TextAnchor anchor, int fontSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.rectTransform.anchoredPosition = anchoredPosition;
            text.rectTransform.sizeDelta = new Vector2(360f, 180f);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<UpgradeButtonState>();

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.10f, 0.12f, 0.92f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            button.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 48f);

            var label = CreateText("Label", buttonObject.transform, Vector2.zero, TextAnchor.MiddleCenter, 15);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.offsetMin = new Vector2(8f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            label.color = Color.white;
            return button;
        }

        private sealed class UpgradeButtonState : MonoBehaviour
        {
            public UpgradeType UpgradeType;
        }
    }
}
