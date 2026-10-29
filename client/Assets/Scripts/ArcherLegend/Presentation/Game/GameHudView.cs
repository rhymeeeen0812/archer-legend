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
    /// 見た目はプレハブ（Resources/UI/GameHudView）で編集します。
    /// </summary>
    public sealed class GameHudView : MonoBehaviour, IGameHudView
    {
        public event Action<int> UpgradeSelected;

        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("レベルアップ時に表示する、画面全体を暗くするパネルです。")]
        [SerializeField] private GameObject upgradePanel;

        [Tooltip("強化の選択肢のボタンです。upgradeLabels と同じ順番・同じ数にします。")]
        [SerializeField] private Button[] upgradeButtons;
        [SerializeField] private TextMeshProUGUI[] upgradeLabels;

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
                    upgradeLabels[i].text = $"<size=22><b>{options[i].Title}</b></size>\n{options[i].Description}";
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
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var index = i;
                upgradeButtons[i].onClick.AddListener(() => UpgradeSelected?.Invoke(index));
            }

            HideUpgrades();
        }
    }
}
