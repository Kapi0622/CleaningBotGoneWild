using CleaningBot.Data;

namespace CleaningBot.Score
{
    /// <summary>
    /// スコアと StageData からスター数（1〜3）を計算する純粋クラス。
    /// ロジックはここに集約し、ScoreModel・Presenter はランク計算を知らない。
    /// </summary>
    public class RankCalculator
    {
        public int Calculate(int score, StageData data)
        {
            if (score >= data.scoreStar3Threshold) return 3;
            if (score >= data.scoreStar2Threshold) return 2;
            return 1;
        }
    }
}
