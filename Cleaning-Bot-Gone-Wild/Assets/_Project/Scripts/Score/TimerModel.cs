using R3;
using UnityEngine;

namespace CleaningBot.Score
{
    /// <summary>
    /// 残り時間を保持する Model。
    /// Tick() は GameTimer（MonoBehaviour）から毎フレーム呼ばれる。
    /// STEP 9 で InGameState が GameTimer.Start/Stop を制御する。
    /// </summary>
    public class TimerModel
    {
        public readonly ReactiveProperty<float> RemainingTime;
        public bool IsTimeUp => RemainingTime.Value <= 0f;
        public float ElapsedTime => _initialTime - RemainingTime.Value;

        /// <summary>残り時間が LowTimeThreshold 以下になったとき true を発火する。STEP 12 で警告演出に使用する。</summary>
        public Observable<bool> IsLowTime
            => RemainingTime.Select(t => t <= LowTimeThreshold).DistinctUntilChanged();

        private const float LowTimeThreshold = 10f;
        private readonly float _initialTime;

        public TimerModel(float initialTime)
        {
            _initialTime = initialTime;
            RemainingTime = new ReactiveProperty<float>(initialTime);
        }

        public void Tick(float deltaTime)
            => RemainingTime.Value = Mathf.Max(0f, RemainingTime.Value - deltaTime);

        public void Reset() => RemainingTime.Value = _initialTime;
    }
}
