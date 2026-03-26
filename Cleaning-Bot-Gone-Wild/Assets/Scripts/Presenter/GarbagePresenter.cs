using R3;
using CleaningBot.Score;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// GarbageModel と GarbageView をバインドする Presenter。
    /// Subscribe と AddTo のみ。ビジネスロジックは持たない。
    /// </summary>
    public class GarbagePresenter
    {
        public void Initialize(GarbageModel model, GarbageView view)
        {
            model.RemainingCount
                .Subscribe(v => view.UpdateGarbageCount(v))
                .AddTo(view);
        }
    }
}
