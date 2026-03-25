using R3;
using CleaningBot.Garbage;

namespace CleaningBot.Score
{
    /// <summary>
    /// 残りゴミ数の ReactiveProperty と除去通知 Subject を持つ Model。
    /// カウントの増減責任は GarbageTracker が持つ。
    /// </summary>
    public class GarbageModel
    {
        public readonly ReactiveProperty<int> RemainingCount = new(0);
        public readonly Subject<GarbageBase> OnGarbageRemoved = new();

        public void SetInitialCount(int count) => RemainingCount.Value = count;

        public void Reset() => RemainingCount.Value = 0;
    }
}
