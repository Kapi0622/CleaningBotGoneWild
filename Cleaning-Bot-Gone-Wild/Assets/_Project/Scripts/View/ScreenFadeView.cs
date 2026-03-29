using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 全画面フェードイン/アウト。
    /// CanvasGroup ベースで子要素全体の alpha を一括制御する。
    /// 最前面 Canvas に全画面黒 Image + CanvasGroup を配置して使う。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFadeView : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private MotionHandle _handle;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            // 起動時は真っ黒(alpha=1)で開始
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        /// <summary>黒 → 透明（ゲーム開始時など）。</summary>
        public async UniTask FadeIn(float duration, CancellationToken ct = default)
        {
            if (_handle.IsActive()) _handle.Cancel();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _handle = LMotion.Create(1f, 0f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_canvasGroup);
            await _handle.ToUniTask(ct);
            _canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 透明 → 黒 → onMidpoint コールバック → 黒 → 透明（リトライ時など）。
        /// onMidpoint は画面が完全に黒い状態で呼ばれる。
        /// </summary>
        public async UniTask FadeOutIn(float halfDuration, Action onMidpoint, CancellationToken ct = default)
        {
            if (_handle.IsActive()) _handle.Cancel();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _handle = LMotion.Create(0f, 1f, halfDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_canvasGroup);
            await _handle.ToUniTask(ct);
            onMidpoint?.Invoke();
            _handle = LMotion.Create(1f, 0f, halfDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_canvasGroup);
            await _handle.ToUniTask(ct);
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if (_handle.IsActive()) _handle.Cancel();
        }
    }
}
