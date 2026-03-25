using R3;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 残りゴミ数の追跡とクリア判定。
    /// STEP 4: クリア時は Debug.Log のみ。
    /// STEP 8: SetDependencies() に ScoreModel を追加して AddMainScore を有効化。
    /// STEP 9: SetDependencies() に GameStateController を追加して ClearState 遷移を有効化。
    /// </summary>
    public class GarbageTracker
    {
        private readonly GarbageModel _model;

        public GarbageTracker(GarbageModel model) => _model = model;

        public void Initialize()
        {
            _model.OnGarbageRemoved
                .Subscribe(g =>
                {
                    _model.RemainingCount.Value--;
                    // STEP 8: _scoreModel?.AddMainScore(g.Data?.scoreValue ?? 0);
                    Debug.Log($"[GarbageTracker] Remaining: {_model.RemainingCount.Value}");
                    if (_model.RemainingCount.Value > 0) return;

                    // STEP 9: _stateCtrl.ChangeState(new ClearState());
                    Debug.Log("[GarbageTracker] All cleared! (STEP 9 で ClearState へ遷移)");
                });
        }
    }
}
