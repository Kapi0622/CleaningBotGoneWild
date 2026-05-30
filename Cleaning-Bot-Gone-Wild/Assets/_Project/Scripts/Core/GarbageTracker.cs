using System;
using R3;
using CleaningBot.Data;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 残りゴミ数の追跡・スコア加算・クリア判定。
    /// コアゴミ全消去時、残り時間があれば BonusState へ、なければ ClearState へ遷移する。
    /// IDisposable を実装し、ライフサイクル終了時に購読を確実に解除できる。
    /// </summary>
    public class GarbageTracker : IDisposable
    {
        private readonly GarbageModel        _model;
        private readonly ScoreModel          _scoreModel;
        private readonly GameStateController _stateController;
        private readonly TimerModel          _timerModel;
        private readonly StageData           _stageData;
        private readonly BonusGarbageSpawner _bonusSpawner;
        private IDisposable _subscription;

        public GarbageTracker(
            GarbageModel        model,
            ScoreModel          scoreModel,
            GameStateController stateController,
            TimerModel          timerModel,
            StageData           stageData,
            BonusGarbageSpawner bonusSpawner)
        {
            _model           = model;
            _scoreModel      = scoreModel;
            _stateController = stateController;
            _timerModel      = timerModel;
            _stageData       = stageData;
            _bonusSpawner    = bonusSpawner;
        }

        public void Initialize()
        {
            _subscription?.Dispose();
            _subscription = _model.OnGarbageRemoved
                .Subscribe(g =>
                {
                    _model.RemainingCount.Value--;
                    _scoreModel.AddMainScore(g.Data?.scoreValue ?? 0);
                    Debug.Log($"[GarbageTracker] Remaining: {_model.RemainingCount.Value}");
                    if (_model.RemainingCount.Value > 0) return;
                    if (!(_stateController.CurrentState is InGameState)) return;

                    if (!_timerModel.IsTimeUp && _stageData.hasBonusPhase)
                    {
                        _scoreModel.TakeCoreClearSnapshot(
                            _timerModel.RemainingTime.Value,
                            _timerModel.ElapsedTime);
                        _stateController.ChangeState(new BonusState(_bonusSpawner));
                    }
                    else
                    {
                        _stateController.ChangeState(new ClearState());
                    }
                });
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
