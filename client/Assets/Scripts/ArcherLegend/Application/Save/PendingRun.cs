using System;

namespace ArcherLegend.Application
{
    /// <summary>
    /// サーバーへまだ送れていないプレイ結果です。オフラインでも遊べるよう、送信できるまでセーブデータに残します。
    /// </summary>
    [Serializable]
    public sealed class PendingRun
    {
        /// <summary>同じ結果を再送してもサーバー側で二重に記録されないようにするための Id です。</summary>
        public string runId;
        public int score;
        public int wave;
        public int enemiesDefeated;
        public int level;
    }
}
