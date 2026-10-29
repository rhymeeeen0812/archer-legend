using System;
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
    /// </summary>
    public sealed class HomeView : MonoBehaviour, IHomeView
    {
        public event Action PlayClicked;
        public event Action BackToTitleClicked;

        private Text recordsText;

        public void ShowLoading()
        {
            recordsText.text = "Loading...";
        }

        public void ShowRecords(int bestScore, int bestWave, int totalEnemiesDefeated, int playCount)
        {
            recordsText.text =
                $"Best Score    {bestScore}\n" +
                $"Best Wave    {bestWave}\n" +
                $"Enemies Defeated    {totalEnemiesDefeated}\n" +
                $"Plays    {playCount}";
        }

        public void ShowLoadError()
        {
            recordsText.text = "Failed to load save data.";
        }

        private void Awake()
        {
            UiFactory.CreateMenuCamera();
            UiFactory.SetupCanvas(gameObject);

            var title = UiFactory.CreateText(transform, "Title", 40);
            title.text = "Home";
            UiFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 190f), new Vector2(620f, 60f));

            recordsText = UiFactory.CreateText(transform, "Records", 22);
            recordsText.lineSpacing = 1.3f;
            UiFactory.PlaceCenter(recordsText.rectTransform, new Vector2(0f, 50f), new Vector2(620f, 180f));

            var playButton = UiFactory.CreateButton(transform, "Play Button", "Adventure", UiFactory.PrimaryButtonColor, 22);
            UiFactory.PlaceCenter(playButton.GetComponent<RectTransform>(), new Vector2(0f, -110f), new Vector2(240f, 54f));
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());

            var titleButton = UiFactory.CreateButton(transform, "Back To Title Button", "Title", UiFactory.SecondaryButtonColor, 18);
            UiFactory.PlaceCenter(titleButton.GetComponent<RectTransform>(), new Vector2(0f, -176f), new Vector2(180f, 44f));
            titleButton.onClick.AddListener(() => BackToTitleClicked?.Invoke());
        }
    }
}
