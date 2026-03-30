using UnityEngine;

namespace CleaningBot.Score
{
    /// <summary>
    /// 残り時間ボーナス倍率を計算し、最終スコアを返す純粋クラス。
    /// 倍率 = 1.0 〜 maxMultiplier（残り時間が多いほど高倍率）
    /// クリア時のみ ResultPresenter から呼ばれる。
    /// </summary>
    public class TimeBonusCalculator
    {
        public int Calculate(int baseScore, float remainingTime, float timeLimit, float maxMultiplier)
        {
            float ratio = timeLimit > 0f ? remainingTime / timeLimit : 0f;
            float multiplier = 1.0f + Mathf.Clamp01(ratio) * (maxMultiplier - 1f);
            return Mathf.RoundToInt(baseScore * multiplier);
        }
    }
}
