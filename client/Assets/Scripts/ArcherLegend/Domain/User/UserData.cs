using System;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// ランをまたいで残るユーザーデータです。セーブ対象の値を Presenter に散らさないための置き場です。
    /// </summary>
    public sealed class UserData : GameEntityBase
    {
        public UserData() : this(0, 0, 0, 0, 0)
        {
        }

        public UserData(int bestScore, int bestWave, int totalScore, int totalEnemiesDefeated, int playCount) : base("local-user")
        {
            BestScore = Math.Max(0, bestScore);
            BestWave = Math.Max(0, bestWave);
            TotalScore = Math.Max(0, totalScore);
            TotalEnemiesDefeated = Math.Max(0, totalEnemiesDefeated);
            PlayCount = Math.Max(0, playCount);
        }

        public int BestScore { get; private set; }
        public int BestWave { get; private set; }
        public int TotalScore { get; private set; }
        public int TotalEnemiesDefeated { get; private set; }
        public int PlayCount { get; private set; }

        /// <summary>
        /// ラン終了時に 1 度だけ呼び、自己ベストと累計を更新します。
        /// </summary>
        public RunRecordResult RecordRun(RunResult run)
        {
            ThrowIfDisposed();
            var isNewBestScore = run.Score > BestScore;
            var isNewBestWave = run.Wave > BestWave;

            if (isNewBestScore)
            {
                BestScore = run.Score;
            }

            if (isNewBestWave)
            {
                BestWave = run.Wave;
            }

            TotalScore += run.Score;
            TotalEnemiesDefeated += run.EnemiesDefeated;
            PlayCount++;
            return new RunRecordResult(isNewBestScore, isNewBestWave);
        }
    }
}
