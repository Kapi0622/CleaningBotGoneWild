using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Score;
using CleaningBot.Stage;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// リトライ時の全リセットを一括オーケストレーションする純粋クラス。
    /// 全 Model・FloorGrid・StageInitializer・GarbageTracker・プレイヤー位置を順番にリセットし、
    /// 最後に GameStateController を InGameState へ遷移させる。
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
            GameStateController gameStateController)
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
        }

        public void Reset()
        {
            _scoreModel.Reset();
            _timerModel.Reset();
            _weaponModel.Reset();
            _garbageModel.Reset();
            _floorGrid.Reset();
            _stageInitializer.ReInitialize();
            _garbageTracker.Initialize();
            _playerTransform.position = _stageData.playerStartPosition;
            _gameStateController.ChangeState(new InGameState());
        }
    }
}
