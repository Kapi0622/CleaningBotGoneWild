using CleaningBot.Data;

namespace CleaningBot.Score
{
    /// <summary>
    /// スコアと StageData からスター数（0〜3）を計算する純粋クラス。
    /// ロジックはここに集約し、ScoreModel・Presenter はランク計算を知らない。
    /// isCleared=false（タイムアップ失敗）のとき 0 を返す。
    /// </summary>
    public class RankCalculator
    {
        public int Calculate(int score, StageData data, bool isCleared)
        {
            if (!isCleared) return 0;
            if (score >= data.scoreStar3Threshold) return 3;
            if (score >= data.scoreStar2Threshold) return 2;
            return 1;
        }
    }
}
