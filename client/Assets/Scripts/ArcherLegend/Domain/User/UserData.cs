namespace ArcherLegend.Domain
{
    /// <summary>
    /// ランに依存しないユーザーデータです。セーブ対象の値を Presenter に散らさないための置き場です。
    /// </summary>
    public sealed class UserData : GameEntityBase
    {
        public UserData() : base("local-user")
        {
        }

        public int TotalScore { get; private set; }
        public int BestWave { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int UpgradesPicked { get; private set; }

        public void RegisterEnemyDefeated(int score, int wave)
        {
            ThrowIfDisposed();
            TotalScore += score;
            EnemiesDefeated++;
            if (wave > BestWave)
            {
                BestWave = wave;
            }
        }

        public void RegisterUpgradePicked()
        {
            ThrowIfDisposed();
            UpgradesPicked++;
        }
    }
}
