using System.Threading;
using R3;
using CleaningBot.Core;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    public class TitlePresenter
    {
        public void Initialize(TitleView view, SceneTransition transition, CancellationToken ct)
        {
            view.OnStartClicked
                .Subscribe(_ => transition.LoadSceneAsync(SceneNames.StageSelect, ct).ForgetWithLog())
                .AddTo(view);
        }
    }
}
