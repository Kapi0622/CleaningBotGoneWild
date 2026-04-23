using UnityEngine;

namespace CleaningBot.Stage
{
    /// <summary>
    /// ステージの解放状態とベストランクを PlayerPrefs に永続化する純粋クラス。
    /// Stage 0（ステージ1）は常に解放扱い。
    /// </summary>
    public class StageProgressStore
    {
        private const string UnlockedKeyPrefix = "stage_unlocked_";
        private const string RankKeyPrefix     = "stage_rank_";

        public bool IsUnlocked(int stageIndex)
        {
            if (stageIndex == 0) return true;
            return PlayerPrefs.GetInt(UnlockedKeyPrefix + stageIndex, 0) == 1;
        }

        public void Unlock(int stageIndex)
        {
            if (stageIndex == 0) return;
            PlayerPrefs.SetInt(UnlockedKeyPrefix + stageIndex, 1);
            PlayerPrefs.Save();
        }

        public int GetBestRank(int stageIndex)
        {
            return PlayerPrefs.GetInt(RankKeyPrefix + stageIndex, 0);
        }

        public void SaveRank(int stageIndex, int rank)
        {
            var current = GetBestRank(stageIndex);
            if (rank <= current) return;
            PlayerPrefs.SetInt(RankKeyPrefix + stageIndex, rank);
            PlayerPrefs.Save();
        }
    }
}
