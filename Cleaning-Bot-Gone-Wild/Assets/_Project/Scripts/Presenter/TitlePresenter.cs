using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using CleaningBot.Core;
using CleaningBot.View;
using UnityEngine;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// TitleView のスタートボタンを購読し、StageSelectScene へ遷移する。
    /// Presenter には Subscribe と AddTo のみ。ロジック禁止。
    /// </summary>
    public class TitlePresenter
    {
        public void Initialize(TitleView view, SceneTransition transition, CancellationToken ct)
        {
            view.OnStartClicked
                .Subscribe(_ => transition.LoadSceneAsync("StageSelectScene", ct)
                    .Forget(ex =>
                    {
                        if (ex is OperationCanceledException) return;
                        Debug.LogException(ex);
                    }))
                .AddTo(view);
        }
    }
}
