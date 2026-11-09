namespace ArcherLegend.Domain
{
    /// <summary>
    /// 通算のウェーブ数と、ステージの対応です。1 つのステージ（部屋）は決まった数のウェーブでできています。
    /// サーバーやセーブデータには今までどおり通算のウェーブ数を記録し、ステージはそこから求めます。
    /// </summary>
    public static class StageProgress
    {
        /// <summary>1 つのステージのウェーブ数です。</summary>
        public const int WavesPerStage = 3;

        /// <summary>通算のウェーブが、何番目のステージにあたるかを返します（1 始まり）。</summary>
        public static int StageOf(int wave) => (Clamp(wave) - 1) / WavesPerStage + 1;

        /// <summary>通算のウェーブが、そのステージの何番目のウェーブかを返します（1 始まり）。</summary>
        public static int WaveInStage(int wave) => (Clamp(wave) - 1) % WavesPerStage + 1;

        /// <summary>そのステージの最後のウェーブかを返します。倒しきると出口が開きます。</summary>
        public static bool IsLastWaveOfStage(int wave) => WaveInStage(wave) == WavesPerStage;

        private static int Clamp(int wave) => wave < 1 ? 1 : wave;
    }
}
