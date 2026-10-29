using System.Collections.Generic;

namespace ArcherLegend.Application
{
    public readonly struct RankingEntry
    {
        public RankingEntry(int rank, string name, int bestScore, int bestWave, bool isMe)
        {
            Rank = rank;
            Name = name;
            BestScore = bestScore;
            BestWave = bestWave;
            IsMe = isMe;
        }

        public int Rank { get; }
        public string Name { get; }
        public int BestScore { get; }
        public int BestWave { get; }
        public bool IsMe { get; }
    }

    public sealed class Ranking
    {
        public Ranking(IReadOnlyList<RankingEntry> entries, RankingEntry? me)
        {
            Entries = entries;
            Me = me;
        }

        /// <summary>上位のプレイヤーです。</summary>
        public IReadOnlyList<RankingEntry> Entries { get; }

        /// <summary>自分の順位です。まだスコアを送っていなければ null です。</summary>
        public RankingEntry? Me { get; }
    }
}
