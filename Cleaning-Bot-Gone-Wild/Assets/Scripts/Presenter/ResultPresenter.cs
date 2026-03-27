using R3;
using CleaningBot.Core;
using CleaningBot.Data;
using CleaningBot.Score;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// GameStateController の OnStateChanged を購読し、
    /// 各 Model から ResultData を組み立てて ResultView に渡す。
    /// リトライボタンの購読も担当する。
    /// PresenterにはSubscribeとAddToしか書かない。
    /// </summary>
    public class ResultPresenter
    {
        public void Initialize(
            GameStateController stateCtrl,
            ScoreModel scoreModel,
            GarbageModel garbageModel,
            TimerModel timerModel,
            RankCalculator rankCalc,
            StageData stageData,
            StageResetter stageResetter,
            ResultView view)
        {
            stateCtrl.OnStateChanged
                .Subscribe(state =>
                {
                    if (state is InGameState) { view.Hide(); return; }

                    var data = new ResultData(
                        scoreModel.MainScore.Value,
                        scoreModel.SubScore.Value,
                        garbageModel.RemainingCount.Value,
                        timerModel.ElapsedTime,
                        rankCalc.Calculate(scoreModel.MainScore.Value, stageData));

                    if (state is ClearState) view.ShowClear(data);
                    else if (state is FailState) view.ShowFail(data);
                })
                .AddTo(view);

            view.OnRetryClicked
                .Subscribe(_ => stageResetter.Reset())
                .AddTo(view);
        }
    }
}
