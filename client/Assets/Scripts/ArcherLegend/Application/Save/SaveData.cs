using System;
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
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int bestScore;
        public int bestWave;
        public int totalScore;
        public int totalEnemiesDefeated;
        public int playCount;

        public static SaveData FromUserData(UserData userData)
        {
            return new SaveData
            {
                bestScore = userData.BestScore,
                bestWave = userData.BestWave,
                totalScore = userData.TotalScore,
                totalEnemiesDefeated = userData.TotalEnemiesDefeated,
                playCount = userData.PlayCount
            };
        }

        public UserData ToUserData()
        {
            return new UserData(bestScore, bestWave, totalScore, totalEnemiesDefeated, playCount);
        }
    }
}
