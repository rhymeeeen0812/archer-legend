using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Home
{
    public interface IHomeView
    {
        event Action PlayClicked;
        event Action BackToTitleClicked;

        void ShowLoading();
        void ShowRecords(int bestScore, int bestWave, int totalEnemiesDefeated, int playCount);
        void ShowLoadError();
    }

    /// <summary>
    /// ホーム画面の表示です。戦績は Presenter から渡された値をそのまま並べます。
    /// 見た目はプレハブ（Resources/UI/HomeView）で編集します。
    /// </summary>
    public sealed class HomeView : MonoBehaviour, IHomeView
    {
        public event Action PlayClicked;
        public event Action BackToTitleClicked;

        [SerializeField] private TextMeshProUGUI recordsText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button backToTitleButton;

        public void ShowLoading()
        {
            recordsText.text = "読み込み中…";
        }

        public void ShowRecords(int bestScore, int bestWave, int totalEnemiesDefeated, int playCount)
        {
            recordsText.text =
                $"ベストスコア    {bestScore}\n" +
                $"最高到達ウェーブ    {bestWave}\n" +
                $"累計撃破数    {totalEnemiesDefeated}\n" +
                $"プレイ回数    {playCount}";
        }

        public void ShowLoadError()
        {
            recordsText.text = "セーブデータを読み込めませんでした";
        }

        private void Awake()
        {
            MenuCamera.Create();
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            backToTitleButton.onClick.AddListener(() => BackToTitleClicked?.Invoke());
        }
    }
}
