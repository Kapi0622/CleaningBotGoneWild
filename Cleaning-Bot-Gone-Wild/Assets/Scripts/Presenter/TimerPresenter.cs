using R3;
using CleaningBot.Score;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// TimerModel と TimerView をバインドする Presenter。
    /// Subscribe と AddTo のみ。ビジネスロジックは持たない。
    /// </summary>
    public class TimerPresenter
    {
        public void Initialize(TimerModel model, TimerView view)
        {
            model.RemainingTime
                .Subscribe(v => view.UpdateTimer(v))
                .AddTo(view);
        }
    }
}
