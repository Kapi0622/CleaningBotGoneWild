using System.Threading;
using R3;
using CleaningBot.Core;
using CleaningBot.Data;
using CleaningBot.Score;
using CleaningBot.View;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            TimeBonusCalculator timeBonusCalc,
            StageData stageData,
            StageResetter stageResetter,
            SceneTransition sceneTransition,
            ResultView view,
            CancellationToken ct)
        {
            stateCtrl.OnStateChanged
                .Subscribe(state =>
                {
                    if (state is InGameState || state is BonusState) { view.Hide(); return; }

                    var isCleared = state is ClearState;

                    // ボーナスフェーズ経由のクリアはコアクリア時点の値を使って時間ボーナスを確定する
                    var remainingForBonus = scoreModel.HasCoreClearSnapshot
                        ? scoreModel.CoreClearRemainingTime
                        : timerModel.RemainingTime.Value;
                    var elapsedForResult = scoreModel.HasCoreClearSnapshot
                        ? scoreModel.CoreClearElapsedTime
                        : timerModel.ElapsedTime;

                    var finalScore = isCleared
                        ? timeBonusCalc.Calculate(scoreModel.MainScore.Value, remainingForBonus, stageData.timeLimit, stageData.timeBonusMaxMultiplier)
                        : scoreModel.MainScore.Value;

                    var data = new ResultData(
                        finalScore,
                        scoreModel.SubScore.Value,
                        garbageModel.RemainingCount.Value,
                        elapsedForResult,
                        rankCalc.Calculate(finalScore, stageData, isCleared));

                    if (state is ClearState) view.ShowClear(data);
                    else if (state is FailState) view.ShowFail(data);
                })
                .AddTo(view);

            view.OnRetryClicked
                .Subscribe(_ => stageResetter.Reset().ForgetWithLog())
                .AddTo(view);

            view.OnStageSelectClicked
                .Subscribe(_ => sceneTransition.LoadSceneAsync(SceneNames.StageSelect, ct).ForgetWithLog())
                .AddTo(view);
        }
    }
}
