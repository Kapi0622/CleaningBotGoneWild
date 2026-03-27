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

        private readonly Subject<GarbageBase> _onGarbageRemoved = new();
        public Observable<GarbageBase> OnGarbageRemoved => _onGarbageRemoved;

        public void SetInitialCount(int count) => RemainingCount.Value = count;

        public void NotifyRemoved(GarbageBase garbage) => _onGarbageRemoved.OnNext(garbage);

        public void Reset() => RemainingCount.Value = 0;
    }
}
