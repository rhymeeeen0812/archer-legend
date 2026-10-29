namespace SurveyApp.Data;

// ゲームのプレイヤー。ログインは行わず、ゲームが初回起動時に作った Id で識別する。
// Id は本人であることの証明も兼ねるので、ランキングなど他人に見える場所には出さない。
public class Player
{
    public const int NameMaxLength = 20;

    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // 戦績。ゲーム側のセーブデータと同じ項目を、サーバーを正として持つ
    public int BestScore { get; set; }
    public int BestWave { get; set; }
    public long TotalScore { get; set; }
    public int TotalEnemiesDefeated { get; set; }
    public int PlayCount { get; set; }

    // 同点のときは先に達成したプレイヤーを上位にする
    public DateTimeOffset? BestScoreAchievedAt { get; set; }

    // 同じプレイヤーの結果が同時に送られたときに、戦績の更新が消えないようにする（PostgreSQL の xmin）
    public uint Version { get; set; }

    /// <summary>
    /// 1 回分のプレイ結果を戦績に反映し、自己ベストを更新したかを返します。
    /// </summary>
    public (bool IsNewBestScore, bool IsNewBestWave) RecordRun(int score, int wave, int enemiesDefeated, DateTimeOffset now)
    {
        var isNewBestScore = score > BestScore;
        var isNewBestWave = wave > BestWave;

        if (isNewBestScore)
        {
            BestScore = score;
            BestScoreAchievedAt = now;
        }

        if (isNewBestWave)
        {
            BestWave = wave;
        }

        TotalScore += score;
        TotalEnemiesDefeated += enemiesDefeated;
        PlayCount++;
        return (isNewBestScore, isNewBestWave);
    }

    /// <summary>
    /// 前後の空白を除き、1〜20 文字で制御文字を含まない名前だけを受け付けます。
    /// </summary>
    public static bool TryNormalizeName(string? name, out string normalized)
    {
        normalized = name?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= NameMaxLength && !normalized.Any(char.IsControl);
    }
}
