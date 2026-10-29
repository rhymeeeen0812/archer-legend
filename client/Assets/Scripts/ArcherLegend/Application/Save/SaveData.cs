using System;
using System.Collections.Generic;
using ArcherLegend.Domain;

namespace ArcherLegend.Application
{
    /// <summary>
    /// 保存用のデータ形式です。JsonUtility で扱えるよう public フィールドにしています。
    /// ドメインの UserData と分けておき、保存形式の変更がゲームのルールに波及しないようにします。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>2: プレイヤー Id と未送信のプレイ結果を追加</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int bestScore;
        public int bestWave;
        public int totalScore;
        public int totalEnemiesDefeated;
        public int playCount;

        // バージョン 1 のデータでは空になる。読み込み時に Id を作り直す
        public string playerId;
        public string playerName;
        public bool isRegistered;
        public List<PendingRun> pendingRuns = new List<PendingRun>();

        public UserData ToUserData()
        {
            return new UserData(bestScore, bestWave, totalScore, totalEnemiesDefeated, playCount);
        }
    }
}
