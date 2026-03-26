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
            // タイマーは InGameState.OnEnter() の StartTimer() で開始する
        }

        public void StartTimer() => _isRunning = true;

        public void StopTimer() => _isRunning = false;

        private void Update()
        {
            if (_isRunning)
                _model?.Tick(Time.deltaTime);
        }
    }
}
