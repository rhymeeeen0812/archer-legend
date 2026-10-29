using System;
using System.Collections.Generic;
using ArcherLegend.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Game
{
    public interface IGameHudView
    {
        /// <summary>選ばれた強化の選択肢の番号（0 始まり）を通知します。</summary>
        event Action<int> UpgradeSelected;

        void SetStatus(int health, int maxHealth, int level, int experience, int experienceToNext, int wave, int score, int aliveEnemies);
        void ShowUpgrades(IReadOnlyList<UpgradeOption> options);
        void HideUpgrades();
    }

    /// <summary>
    /// プレイ中の HUD です。セッションは参照せず、Presenter から渡された値を表示するだけにします。
    /// </summary>
    public sealed class GameHudView : MonoBehaviour, IGameHudView
    {
        private const int UpgradeSlotCount = 3;

        public event Action<int> UpgradeSelected;

        private TextMeshProUGUI statusText;
        private GameObject upgradePanel;
        private Button[] upgradeButtons;

        public void SetStatus(int health, int maxHealth, int level, int experience, int experienceToNext, int wave, int score, int aliveEnemies)
        {
            statusText.text = $"HP {health}/{maxHealth}\nレベル {level}  経験値 {experience}/{experienceToNext}\nウェーブ {wave}  スコア {score}\n残りの敵 {aliveEnemies}";
        }

        public void ShowUpgrades(IReadOnlyList<UpgradeOption> options)
        {
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var hasOption = i < options.Count;
                upgradeButtons[i].gameObject.SetActive(hasOption);
                if (hasOption)
                {
                    upgradeButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = $"<size=22><b>{options[i].Title}</b></size>\n{options[i].Description}";
                }
            }

            upgradePanel.SetActive(true);
        }

        public void HideUpgrades()
        {
            upgradePanel.SetActive(false);
        }

        private void Awake()
        {
            UiFactory.SetupCanvas(gameObject);

            statusText = UiFactory.CreateText(transform, "Status", 20, TextAlignmentOptions.TopLeft);
            UiFactory.PlaceTopLeft(statusText.rectTransform, new Vector2(12f, -12f), new Vector2(360f, 180f));

            var controlsHint = UiFactory.CreateText(transform, "Controls Hint", 16, TextAlignmentOptions.Bottom);
            controlsHint.text = "WASD / 矢印キーで移動　立ち止まると近くの敵へ自動で矢を放つ";
            controlsHint.color = new Color(1f, 1f, 1f, 0.7f);
            UiFactory.Stretch(controlsHint.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));

            // ゲームが一時停止していることが伝わるよう、画面全体を暗くしてから選択肢を出す
            upgradePanel = UiFactory.CreatePanel(transform, "Upgrade Panel", new Color(0f, 0f, 0f, 0.55f)).gameObject;

            var heading = UiFactory.CreateText(upgradePanel.transform, "Heading", 40);
            heading.text = "レベルアップ！";
            heading.color = UiFactory.HighlightColor;
            UiFactory.PlaceCenter(heading.rectTransform, new Vector2(0f, 170f), new Vector2(620f, 56f));

            var guide = UiFactory.CreateText(upgradePanel.transform, "Guide", 18);
            guide.text = "強化を1つ選んでください";
            UiFactory.PlaceCenter(guide.rectTransform, new Vector2(0f, 124f), new Vector2(620f, 30f));

            upgradeButtons = new Button[UpgradeSlotCount];
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var index = i;
                var button = UiFactory.CreateButton(upgradePanel.transform, $"Upgrade {i + 1}", string.Empty, UiFactory.PrimaryButtonColor, 16);
                UiFactory.PlaceCenter(button.GetComponent<RectTransform>(), new Vector2(0f, 50f - i * 84f), new Vector2(420f, 72f));
                button.onClick.AddListener(() => UpgradeSelected?.Invoke(index));
                upgradeButtons[i] = button;
            }

            HideUpgrades();
        }
    }
}
