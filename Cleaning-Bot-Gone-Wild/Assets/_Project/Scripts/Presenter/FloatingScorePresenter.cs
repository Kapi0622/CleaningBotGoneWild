using CleaningBot.Score;
using CleaningBot.View;
using R3;
using UnityEngine;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// GarbageModel.OnGarbageRemoved を購読し、
    /// 除去されたゴミの位置とスコアを FloatingScoreView に渡す。
    /// </summary>
    public class FloatingScorePresenter
    {
        public void Initialize(GarbageModel garbageModel, FloatingScoreView view, MonoBehaviour lifetimeOwner)
        {
            garbageModel.OnGarbageRemoved
                .Subscribe(garbage =>
                {
                    var pos = garbage.transform.position;
                    var score = garbage.Data != null ? garbage.Data.scoreValue : 0;
                    if (score > 0)
                        view.Show(pos, score);
                })
                .AddTo(lifetimeOwner);
        }
    }
}
