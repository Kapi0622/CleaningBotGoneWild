using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using CleaningBot.Core;
using CleaningBot.Data;
using CleaningBot.View;
using UnityEngine;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// StageSelectView の選択・戻るイベントを購読し、シーン遷移を行う。
    /// Presenter には Subscribe と AddTo のみ。ロジック禁止。
    /// </summary>
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
                    transition.LoadSceneAsync("InGameScene", ct)
                        .Forget(ex =>
                        {
                            if (ex is OperationCanceledException) return;
                            Debug.LogException(ex);
                        });
                })
                .AddTo(view);

            view.OnBackClicked
                .Subscribe(_ => transition.LoadSceneAsync("TitleScene", ct)
                    .Forget(ex =>
                    {
                        if (ex is OperationCanceledException) return;
                        Debug.LogException(ex);
                    }))
                .AddTo(view);
        }
    }
}
