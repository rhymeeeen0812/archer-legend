namespace SurveyApp.Data;

// 1 回分のプレイ結果。戦績の集計元であり、運営ツールでの分析にも使う。
public class Run
{
    // ゲーム側で生成する。通信エラーで同じ結果が再送されても二重に記録しないためのキー
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int Score { get; set; }
    public int Wave { get; set; }
    public int EnemiesDefeated { get; set; }
    public int Level { get; set; }

    // 再送されたときに、最初と同じ結果を返せるよう保存しておく
    public bool IsNewBestScore { get; set; }
    public bool IsNewBestWave { get; set; }

    public DateTimeOffset PlayedAt { get; set; }
}
