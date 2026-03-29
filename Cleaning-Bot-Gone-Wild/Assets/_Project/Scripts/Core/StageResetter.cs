using System.Threading;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Score;
using CleaningBot.Stage;
using CleaningBot.View;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// リトライ時の全リセットを一括オーケストレーションする純粋クラス。
    /// フェードアウト → 全リセット + UIアニメリセット → フェードイン → カウントダウン → ゲーム開始。
    /// </summary>
    public class StageResetter
    {
        private readonly ScoreModel          _scoreModel;
        private readonly TimerModel          _timerModel;
        private readonly WeaponModel         _weaponModel;
        private readonly GarbageModel        _garbageModel;
        private readonly FloorGrid           _floorGrid;
        private readonly StageInitializer    _stageInitializer;
        private readonly GarbageTracker      _garbageTracker;
        private readonly Transform           _playerTransform;
        private readonly StageData           _stageData;
        private readonly GameStateController _gameStateController;
        private readonly ScreenFadeView      _screenFadeView;
        private readonly CountdownView       _countdownView;
        private readonly ResultView          _resultView;
        private readonly TimerView           _timerView;
        private readonly GarbageView         _garbageView;

        private CancellationTokenSource _cts = new();

        public StageResetter(
            ScoreModel          scoreModel,
            TimerModel          timerModel,
            WeaponModel         weaponModel,
            GarbageModel        garbageModel,
            FloorGrid           floorGrid,
            StageInitializer    stageInitializer,
            GarbageTracker      garbageTracker,
            Transform           playerTransform,
            StageData           stageData,
            GameStateController gameStateController,
            ScreenFadeView      screenFadeView,
            CountdownView       countdownView,
            ResultView          resultView,
            TimerView           timerView,
            GarbageView         garbageView)
        {
            _scoreModel          = scoreModel;
            _timerModel          = timerModel;
            _weaponModel         = weaponModel;
            _garbageModel        = garbageModel;
            _floorGrid           = floorGrid;
            _stageInitializer    = stageInitializer;
            _garbageTracker      = garbageTracker;
            _playerTransform     = playerTransform;
            _stageData           = stageData;
            _gameStateController = gameStateController;
            _screenFadeView      = screenFadeView;
            _countdownView       = countdownView;
            _resultView          = resultView;
            _timerView           = timerView;
            _garbageView         = garbageView;
        }

        public async UniTask Reset()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            await _screenFadeView.FadeOutIn(0.3f, () =>
            {
                // 全 Model リセット
                _scoreModel.Reset();
                _timerModel.Reset();
                _weaponModel.Reset();
                _garbageModel.Reset();
                _floorGrid.Reset();
                _stageInitializer.ReInitialize();
                _garbageTracker.Initialize();
                _playerTransform.position = _stageData.playerStartPosition;

                // UI アニメーションリセット
                _timerView.ResetAnimation();
                _garbageView.ResetAnimation();

                // 画面が黒い間にリザルトパネルを非表示・世界を停止
                _resultView.Hide();
                Time.timeScale = 0f;
            }, ct);

            try
            {
                await _countdownView.PlayCountdownAsync(ct);
            }
            finally
            {
                Time.timeScale = 1f;
            }
            _gameStateController.ChangeState(new InGameState());
        }
    }
}
