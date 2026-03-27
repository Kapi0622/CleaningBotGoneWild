using R3;
using CleaningBot.Score;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// ScoreModel と ScoreView をバインドする Presenter。
    /// Subscribe と AddTo のみ。ビジネスロジックは持たない。
    /// </summary>
    public class ScorePresenter
    {
        public void Initialize(ScoreModel model, ScoreView view)
        {
            model.MainScore
                .Subscribe(v => view.UpdateMainScore(v))
                .AddTo(view);

            model.SubScore
                .Subscribe(v => view.UpdateSubScore(v))
                .AddTo(view);
        }
    }
}
