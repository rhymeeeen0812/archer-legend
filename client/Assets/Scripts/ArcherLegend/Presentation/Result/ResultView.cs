using System;
using TMPro;
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
    /// 見た目はプレハブ（Resources/UI/ResultView）で編集します。
    /// </summary>
    public sealed class ResultView : MonoBehaviour, IResultView
    {
        private const string NewRecordLabel = "  <color=#FFD54A>自己ベスト更新！</color>";

        public event Action RetryClicked;
        public event Action HomeClicked;

        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI saveErrorText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        public void Show(int score, int wave, int enemiesDefeated, int level, bool isNewBestScore, bool isNewBestWave)
        {
            statsText.text =
                $"スコア    {score}{(isNewBestScore ? NewRecordLabel : string.Empty)}\n" +
                $"到達ウェーブ    {wave}{(isNewBestWave ? NewRecordLabel : string.Empty)}\n" +
                $"撃破数    {enemiesDefeated}\n" +
                $"レベル    {level}";
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
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
            Hide();
        }
    }
}
