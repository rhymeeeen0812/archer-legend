using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Result
{
    public interface IResultView
    {
        event Action RetryClicked;
        event Action HomeClicked;

        void Show(int score, int wave, int enemiesDefeated, int level, bool isNewBestScore, bool isNewBestWave);
        void ShowSaveError();
        void Hide();
    }

    /// <summary>
    /// ゲームオーバー時のリザルト画面です。HUD より手前に重ねて表示します。
    /// </summary>
    public sealed class ResultView : MonoBehaviour, IResultView
    {
        private const string NewRecordLabel = "  <color=#FFD54A>NEW RECORD!</color>";

        public event Action RetryClicked;
        public event Action HomeClicked;

        private GameObject root;
        private Text statsText;
        private Text saveErrorText;

        public void Show(int score, int wave, int enemiesDefeated, int level, bool isNewBestScore, bool isNewBestWave)
        {
            statsText.text =
                $"Score    {score}{(isNewBestScore ? NewRecordLabel : string.Empty)}\n" +
                $"Wave    {wave}{(isNewBestWave ? NewRecordLabel : string.Empty)}\n" +
                $"Enemies Defeated    {enemiesDefeated}\n" +
                $"Level    {level}";
            saveErrorText.gameObject.SetActive(false);
            root.SetActive(true);
        }

        public void ShowSaveError()
        {
            saveErrorText.gameObject.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void Awake()
        {
            UiFactory.SetupCanvas(gameObject, sortingOrder: 10);

            root = UiFactory.CreatePanel(transform, "Overlay", UiFactory.OverlayColor).gameObject;

            var title = UiFactory.CreateText(root.transform, "Title", 44);
            title.text = "Game Over";
            UiFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 180f), new Vector2(620f, 64f));

            statsText = UiFactory.CreateText(root.transform, "Stats", 22);
            statsText.lineSpacing = 1.3f;
            UiFactory.PlaceCenter(statsText.rectTransform, new Vector2(0f, 46f), new Vector2(620f, 170f));

            saveErrorText = UiFactory.CreateText(root.transform, "Save Error", 16);
            saveErrorText.text = "Failed to save your record.";
            saveErrorText.color = UiFactory.HighlightColor;
            UiFactory.PlaceCenter(saveErrorText.rectTransform, new Vector2(0f, -54f), new Vector2(620f, 30f));

            var retryButton = UiFactory.CreateButton(root.transform, "Retry Button", "Retry", UiFactory.PrimaryButtonColor, 22);
            UiFactory.PlaceCenter(retryButton.GetComponent<RectTransform>(), new Vector2(-130f, -120f), new Vector2(220f, 54f));
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());

            var homeButton = UiFactory.CreateButton(root.transform, "Home Button", "Home", UiFactory.SecondaryButtonColor, 22);
            UiFactory.PlaceCenter(homeButton.GetComponent<RectTransform>(), new Vector2(130f, -120f), new Vector2(220f, 54f));
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());

            root.SetActive(false);
        }
    }
}
