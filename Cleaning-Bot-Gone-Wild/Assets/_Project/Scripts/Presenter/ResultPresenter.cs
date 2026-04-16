using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using CleaningBot.Core;
using CleaningBot.Data;
using CleaningBot.Score;
using CleaningBot.View;
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
                    if (state is InGameState) { view.Hide(); return; }

                    var isCleared = state is ClearState;
                    var finalScore = isCleared
                        ? timeBonusCalc.Calculate(scoreModel.MainScore.Value, timerModel.RemainingTime.Value, stageData.timeLimit, stageData.timeBonusMaxMultiplier)
                        : scoreModel.MainScore.Value;

                    var data = new ResultData(
                        finalScore,
                        scoreModel.SubScore.Value,
                        garbageModel.RemainingCount.Value,
                        timerModel.ElapsedTime,
                        rankCalc.Calculate(finalScore, stageData, isCleared));

                    if (state is ClearState) view.ShowClear(data);
                    else if (state is FailState) view.ShowFail(data);
                })
                .AddTo(view);

            view.OnRetryClicked
                .Subscribe(_ => stageResetter.Reset().Forget(ex =>
                {
                    if (ex is OperationCanceledException) return;
                    Debug.LogException(ex);
                }))
                .AddTo(view);

            view.OnStageSelectClicked
                .Subscribe(_ => sceneTransition.LoadSceneAsync("StageSelectScene", ct)
                    .Forget(ex =>
                    {
                        if (ex is OperationCanceledException) return;
                        Debug.LogException(ex);
                    }))
                .AddTo(view);
        }
    }
}
