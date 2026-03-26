using System;
using R3;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 残りゴミ数の追跡・スコア加算・クリア判定。
    /// STEP 9: コンストラクタに GameStateController を追加して ClearState 遷移を有効化。
    /// </summary>
    public class GarbageTracker
    {
        private readonly GarbageModel _model;
        private readonly ScoreModel _scoreModel;
        private IDisposable _subscription;

        public GarbageTracker(GarbageModel model, ScoreModel scoreModel)
        {
            _model = model;
            _scoreModel = scoreModel;
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

                    // STEP 9: _stateCtrl.ChangeState(new ClearState());
                    Debug.Log("[GarbageTracker] All cleared! (STEP 9 で ClearState へ遷移)");
                });
        }
    }
}
