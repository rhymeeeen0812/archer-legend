namespace ArcherLegend.Domain
{
    /// <summary>
    /// 1 回のラン（ゲーム開始からゲームオーバーまで）の結果です。
    /// </summary>
    public readonly struct RunResult
    {
        public RunResult(int score, int wave, int enemiesDefeated, int level)
        {
            Score = score;
            Wave = wave;
            EnemiesDefeated = enemiesDefeated;
            Level = level;
        }

        public int Score { get; }
        public int Wave { get; }
        public int EnemiesDefeated { get; }
        public int Level { get; }
    }

    /// <summary>
    /// ランの結果を記録したときに、自己ベストを更新したかどうかです。
    /// </summary>
    public readonly struct RunRecordResult
    {
        public RunRecordResult(bool isNewBestScore, bool isNewBestWave)
        {
            IsNewBestScore = isNewBestScore;
            IsNewBestWave = isNewBestWave;
        }

        public bool IsNewBestScore { get; }
        public bool IsNewBestWave { get; }
    }
}
