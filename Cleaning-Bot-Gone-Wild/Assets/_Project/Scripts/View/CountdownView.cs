using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// ゲーム開始時の「3, 2, 1, GO!」カウントダウン演出。
    /// PlayCountdownAsync(ct) を await するとカウントダウン完了まで待機する。
    /// </summary>
    public class CountdownView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countdownText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _countdownText.text = "";
        }

        /// <summary>カウントダウンを開始し、完了まで待機できる UniTask を返す。</summary>
        public UniTask PlayCountdownAsync(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            return RunCountdownAsync(_cts.Token);
        }

        private async UniTask RunCountdownAsync(CancellationToken ct)
        {
            _canvasGroup.alpha = 1f;

            for (var i = 3; i >= 1; i--)
            {
                _countdownText.text = i.ToString();
                _countdownText.transform.localScale = Vector3.one * 2f;
                _countdownText.alpha = 1f;

                await LMotion.Create(Vector3.one * 2f, Vector3.zero, 0.7f)
                    .WithEase(Ease.InBack)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .BindToLocalScale(_countdownText.transform)
                    .ToUniTask(ct);

                await UniTask.Delay(100, Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime, cancellationToken: ct);
            }

            // GO!
            _countdownText.text = "GO!";
            _countdownText.transform.localScale = Vector3.zero;
            _countdownText.alpha = 1f;

            await LMotion.Create(Vector3.zero, Vector3.one * 1.5f, 0.3f)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(_countdownText.transform)
                .ToUniTask(ct);

            await UniTask.Delay(300, Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime, cancellationToken: ct);

            await LMotion.Create(1f, 0f, 0.2f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(_canvasGroup)
                .ToUniTask(ct);

            _canvasGroup.alpha = 0f;
        }

        /// <summary>リトライ時等でカウントダウンを中断する。</summary>
        public void Cancel()
        {
            _cts?.Cancel();
            _cts = null;
            _canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
        }
    }
}
