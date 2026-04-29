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

        /// <summary>コアゴミ全消去時点の残り時間。BonusState 経由クリア時の時間ボーナス計算に使う。</summary>
        public float CoreClearRemainingTime { get; private set; } = -1f;
        /// <summary>コアゴミ全消去時点の経過時間。リザルトのクリアタイム表示に使う。</summary>
        public float CoreClearElapsedTime   { get; private set; } = -1f;
        /// <summary>スナップショットが存在するか（ボーナスフェーズ経由でクリアされたか）。</summary>
        public bool  HasCoreClearSnapshot   => CoreClearRemainingTime >= 0;

        public void AddMainScore(int value) => MainScore.Value += value;

        public void AddSubScore(int value)
        {
            SubScore.Value += value;
            _onSubScoreAdded.OnNext(value);
        }

        /// <summary>
        /// GarbageTracker が BonusState 遷移直前に呼ぶ。
        /// コアクリア時点の時間情報を確定させ、ResultPresenter が使えるようにする。
        /// </summary>
        public void TakeCoreClearSnapshot(float remainingTime, float elapsedTime)
        {
            CoreClearRemainingTime = remainingTime;
            CoreClearElapsedTime   = elapsedTime;
        }

        public void Reset()
        {
            MainScore.Value        = 0;
            SubScore.Value         = 0;
            CoreClearRemainingTime = -1f;
            CoreClearElapsedTime   = -1f;
        }
    }
}
