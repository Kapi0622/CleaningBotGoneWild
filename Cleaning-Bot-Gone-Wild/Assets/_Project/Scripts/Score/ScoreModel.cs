using R3;

namespace CleaningBot.Score
{
    /// <summary>
    /// メインスコア・サブスコアを保持する Model。
    /// ロジックは持たず、データと加算メソッドのみ。
    /// STEP 8 で GarbageTracker に注入してメインスコア加算に使用する。
    /// </summary>
    public class ScoreModel
    {
        public readonly ReactiveProperty<int> MainScore = new(0);
        public readonly ReactiveProperty<int> SubScore  = new(0);

        private readonly Subject<int> _onSubScoreAdded = new();
        /// <summary>サブスコアが加算されるたびに加算額を発火する。STEP 12 でフローティングスコア演出に使用する。</summary>
        public Observable<int> OnSubScoreAdded => _onSubScoreAdded;

        public void AddMainScore(int value) => MainScore.Value += value;

        public void AddSubScore(int value)
        {
            SubScore.Value += value;
            _onSubScoreAdded.OnNext(value);
        }

        public void Reset()
        {
            MainScore.Value = 0;
            SubScore.Value  = 0;
        }
    }
}
