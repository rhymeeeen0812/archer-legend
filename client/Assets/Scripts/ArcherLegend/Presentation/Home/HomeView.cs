using System;
using System.Text;
using ArcherLegend.Application;
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
        void ShowRankingLoading();
        void ShowRanking(Ranking ranking);
        void ShowRankingUnavailable();
    }

    /// <summary>
    /// ホーム画面の表示です。戦績は Presenter から渡された値をそのまま並べます。
    /// 見た目はプレハブ（Resources/UI/HomeView）で編集します。
    /// </summary>
    public sealed class HomeView : MonoBehaviour, IHomeView
    {
        public event Action PlayClicked;
        public event Action BackToTitleClicked;

        private const string RankingHeader = "<b>ランキング</b>\n";

        [SerializeField] private TextMeshProUGUI recordsText;
        [SerializeField] private TextMeshProUGUI rankingText;
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

        public void ShowRankingLoading()
        {
            rankingText.text = RankingHeader + "読み込み中…";
        }

        public void ShowRanking(Ranking ranking)
        {
            if (ranking.Entries.Count == 0)
            {
                rankingText.text = RankingHeader + "まだ記録がありません";
                return;
            }

            var builder = new StringBuilder(RankingHeader);
            foreach (var entry in ranking.Entries)
            {
                builder.AppendLine(FormatEntry(entry));
            }

            // 上位に入っていなくても自分の順位は出す
            if (ranking.Me is RankingEntry me && me.Rank > ranking.Entries.Count)
            {
                builder.AppendLine("…");
                builder.AppendLine(FormatEntry(me));
            }

            rankingText.text = builder.ToString().TrimEnd();
        }

        public void ShowRankingUnavailable()
        {
            rankingText.text = RankingHeader + "サーバーに接続できませんでした";
        }

        private static string FormatEntry(RankingEntry entry)
        {
            var line = $"{entry.Rank}位  {entry.Name}  {entry.BestScore}";
            return entry.IsMe ? $"<color=#FFD54A>{line}</color>" : line;
        }

        private void Awake()
        {
            MenuCamera.Create();
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            backToTitleButton.onClick.AddListener(() => BackToTitleClicked?.Invoke());
        }
    }
}
