using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// TimerModel を毎フレーム Tick する MonoBehaviour。
    /// STEP 9 で InGameState.OnEnter() から StartTimer()、
    /// ClearState/FailState.OnEnter() から StopTimer() を呼ぶ。
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        private TimerModel _model;
        private bool _isRunning;

        public void Initialize(TimerModel model)
        {
            _model = model;
            _isRunning = true; // STEP 8: 即時開始（STEP 9 で InGameState から制御）
        }

        /// <summary>STEP 9 で InGameState から呼ぶ。</summary>
        public void StartTimer() => _isRunning = true;

        /// <summary>STEP 9 で ClearState / FailState から呼ぶ。</summary>
        public void StopTimer() => _isRunning = false;

        private void Update()
        {
            if (_isRunning)
                _model?.Tick(Time.deltaTime);
        }
    }
}
