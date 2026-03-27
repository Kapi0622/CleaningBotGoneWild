using System;
using R3;
using CleaningBot.Score;

namespace CleaningBot.Garbage
{
    /// <summary>
    /// ゴミの OnRemoved 購読を一元管理する。
    /// Register() で動的生成ゴミにも対応する。
    /// </summary>
    public class GarbageRegistry
    {
        private readonly GarbageModel _model;

        public GarbageRegistry(GarbageModel model) => _model = model;

        public void Register(GarbageBase garbage)
        {
            if (garbage == null) throw new ArgumentNullException(nameof(garbage));
            garbage.OnRemoved
                .Subscribe(g => _model.NotifyRemoved(g))
                .AddTo(garbage); // ゴミが Destroy されたら自動 Dispose
        }
    }
}
