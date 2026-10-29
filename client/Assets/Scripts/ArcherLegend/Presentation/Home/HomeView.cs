using System;
using TMPro;
using UnityEngine;

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

        private TextMeshProUGUI recordsText;

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
            UiFactory.CreateMenuCamera();
            UiFactory.SetupCanvas(gameObject);

            var title = UiFactory.CreateText(transform, "Title", 40);
            title.text = "ホーム";
            UiFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 190f), new Vector2(620f, 60f));

            recordsText = UiFactory.CreateText(transform, "Records", 22);
            recordsText.lineSpacing = 30f;
            UiFactory.PlaceCenter(recordsText.rectTransform, new Vector2(0f, 50f), new Vector2(620f, 180f));

            var playButton = UiFactory.CreateButton(transform, "Play Button", "冒険に出る", UiFactory.PrimaryButtonColor, 22);
            UiFactory.PlaceCenter(playButton.GetComponent<RectTransform>(), new Vector2(0f, -110f), new Vector2(240f, 54f));
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());

            var titleButton = UiFactory.CreateButton(transform, "Back To Title Button", "タイトルへ", UiFactory.SecondaryButtonColor, 18);
            UiFactory.PlaceCenter(titleButton.GetComponent<RectTransform>(), new Vector2(0f, -176f), new Vector2(180f, 44f));
            titleButton.onClick.AddListener(() => BackToTitleClicked?.Invoke());
        }
    }
}
