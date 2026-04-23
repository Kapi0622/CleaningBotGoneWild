using System.Threading;
using R3;
using CleaningBot.Core;
using CleaningBot.Data;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    public class StageSelectPresenter
    {
        public void Initialize(
            StageSelectView view,
            SelectedStageHolder holder,
            SceneTransition transition,
            CancellationToken ct)
        {
            view.OnStageSelected
                .Subscribe(idx =>
                {
                    holder.SelectedStageIndex = idx;
                    transition.LoadSceneAsync(SceneNames.InGame, ct).ForgetWithLog();
                })
                .AddTo(view);

            view.OnBackClicked
                .Subscribe(_ => transition.LoadSceneAsync(SceneNames.Title, ct).ForgetWithLog())
                .AddTo(view);
        }
    }
}
