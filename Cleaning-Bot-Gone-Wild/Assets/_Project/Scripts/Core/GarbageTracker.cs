using System;
using R3;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 残りゴミ数の追跡・スコア加算・クリア判定。
    /// 全ゴミ除去時に GameStateController.ChangeState(new ClearState()) を呼ぶ。
    /// IDisposable を実装し、ライフサイクル終了時に購読を確実に解除できる。
    /// </summary>
    public class GarbageTracker : IDisposable
    {
        private readonly GarbageModel _model;
        private readonly ScoreModel _scoreModel;
        private readonly GameStateController _stateController;
        private IDisposable _subscription;

        public GarbageTracker(GarbageModel model, ScoreModel scoreModel, GameStateController stateController)
        {
            _model = model;
            _scoreModel = scoreModel;
            _stateController = stateController;
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

                    _stateController.ChangeState(new ClearState());
                });
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
