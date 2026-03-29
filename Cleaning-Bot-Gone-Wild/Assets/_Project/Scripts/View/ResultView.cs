using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CleaningBot.Core;

namespace CleaningBot.View
{
    /// <summary>
    /// クリア・失敗パネルの表示切り替えと ResultData の内容描画を行う View。
    /// クリア: ドロップイン + スコアカウントアップ + 星パラパラ + ボタンパルス。
    /// 失敗: シェイクイン + グレーアウト + ボタン遅延表示。
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _clearPanel;
        [SerializeField] private GameObject _failPanel;

        [Header("Clear Panel")]
        [SerializeField] private RectTransform _clearPanelRect;
        [SerializeField] private TMP_Text _clearTitleText;
        [SerializeField] private TMP_Text _clearMainScoreText;
        [SerializeField] private TMP_Text _clearRankText;
        [SerializeField] private TMP_Text _clearElapsedTimeText;

        [Header("Fail Panel")]
        [SerializeField] private RectTransform _failPanelRect;
        [SerializeField] private CanvasGroup _failCanvasGroup;
        [SerializeField] private TMP_Text _failMainScoreText;
        [SerializeField] private TMP_Text _failRankText;
        [SerializeField] private Image _failOverlay; // 半透明黒オーバーレイ

        [Header("Retry Buttons")]
        [SerializeField] private ClickEvent _clearRetryButton;
        [SerializeField] private ClickEvent _failRetryButton;
        [SerializeField] private CanvasGroup _clearRetryGroup;
        [SerializeField] private CanvasGroup _failRetryGroup;

        private CancellationTokenSource _cts = new();
        private MotionHandle _pulseHandle;
        private Observable<Unit> _onRetryClicked;
        public Observable<Unit> OnRetryClicked => _onRetryClicked ??= BuildOnRetryClicked();

        private Observable<Unit> BuildOnRetryClicked()
        {
            if (_clearRetryButton == null)
                throw new System.InvalidOperationException(
                    $"[ResultView] _clearRetryButton が未アサインです。{gameObject.name} の Inspector を確認してください。");
            if (_failRetryButton == null)
                throw new System.InvalidOperationException(
                    $"[ResultView] _failRetryButton が未アサインです。{gameObject.name} の Inspector を確認してください。");
            return Observable.Merge(_clearRetryButton.OnClicked, _failRetryButton.OnClicked);
        }

        private void Awake()
        {
            _clearPanel.SetActive(false);
            _failPanel.SetActive(false);
        }

        public void ShowClear(ResultData data)
        {
            KillCurrent();
            _failPanel.SetActive(false);

            // 初期化
            _clearMainScoreText.text   = "スコア: 0";
            _clearRankText.text        = "";
            _clearElapsedTimeText.text = $"タイム: {FormatTime(data.ElapsedTime)}";

            _clearRankText.text = "";
            _clearRankText.transform.localScale = Vector3.one;

            if (_clearRetryGroup != null)
                _clearRetryGroup.alpha = 0f;

            if (_clearPanelRect != null)
            {
                var pos = _clearPanelRect.anchoredPosition;
                _clearPanelRect.anchoredPosition = new Vector2(pos.x, 800f);
            }

            _clearPanel.SetActive(true);
            ShowClearAsync(data, _cts.Token).Forget();
        }

        public void ShowFail(ResultData data)
        {
            KillCurrent();
            _clearPanel.SetActive(false);

            _failMainScoreText.text = $"スコア: {data.MainScore}";
            _failRankText.text      = $"{new string('\u2605', data.StarRank)}{new string('\u2606', 3 - data.StarRank)}";

            if (_failCanvasGroup != null)
                _failCanvasGroup.alpha = 0f;
            if (_failOverlay != null)
            {
                var c = _failOverlay.color;
                _failOverlay.color = new Color(c.r, c.g, c.b, 0f);
            }
            if (_failRetryGroup != null)
                _failRetryGroup.alpha = 0f;

            _failPanel.SetActive(true);
            ShowFailAsync(data, _cts.Token).Forget();
        }

        public void Hide()
        {
            KillCurrent();
            _clearPanel.SetActive(false);
            _failPanel.SetActive(false);
        }

        private void KillCurrent()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            if (_pulseHandle.IsActive()) _pulseHandle.Cancel();
            _pulseHandle = default;
            if (_clearRetryButton != null)
                _clearRetryButton.transform.localScale = Vector3.one;
        }

        private async UniTaskVoid ShowClearAsync(ResultData data, CancellationToken ct)
        {
            // (1) パネルドロップイン
            if (_clearPanelRect != null)
            {
                var pos = _clearPanelRect.anchoredPosition;
                await LMotion.Create(pos, new Vector2(pos.x, 0f), 0.6f)
                    .WithEase(Ease.OutBounce)
                    .BindToAnchoredPosition(_clearPanelRect)
                    .ToUniTask(ct);
            }

            // (2) 「クリア!」タイトルバウンス
            if (_clearTitleText != null)
            {
                _clearTitleText.transform.localScale = Vector3.one * 2f;
                await LMotion.Create(Vector3.one * 2f, Vector3.one, 0.5f)
                    .WithEase(Ease.OutBack)
                    .BindToLocalScale(_clearTitleText.transform)
                    .ToUniTask(ct);
            }

            // (3) スコアカウントアップ
            var targetScore = data.MainScore;
            await LMotion.Create(0, targetScore, 0.8f)
                .WithEase(Ease.OutQuad)
                .Bind(x => _clearMainScoreText.text = $"スコア: {x}")
                .ToUniTask(ct);

            // (4) 星パラパラ表示
            _clearRankText.text = "";
            for (var i = 0; i < data.StarRank; i++)
            {
                _clearRankText.text += '\u2605';
                _clearRankText.transform.localScale = Vector3.zero;
                await LMotion.Create(Vector3.zero, Vector3.one, 0.3f)
                    .WithEase(Ease.OutBack)
                    .BindToLocalScale(_clearRankText.transform)
                    .ToUniTask(ct);
            }
            _clearRankText.text += new string('\u2606', 3 - data.StarRank);

            // (5) リトライボタンフェードイン + パルス
            if (_clearRetryGroup != null)
            {
                await LMotion.Create(0f, 1f, 0.3f)
                    .BindToAlpha(_clearRetryGroup)
                    .ToUniTask(ct);

                if (_clearRetryButton != null)
                {
                    _pulseHandle = LMotion.Create(1f, 1.05f, 0.8f)
                        .WithLoops(-1, LoopType.Yoyo)
                        .WithEase(Ease.InOutSine)
                        .BindToLocalScaleXYZ(_clearRetryButton.transform);
                }
            }
        }

        private async UniTaskVoid ShowFailAsync(ResultData data, CancellationToken ct)
        {
            // (1) グレーアウト
            if (_failOverlay != null)
            {
                await LMotion.Create(0f, 0.6f, 0.5f)
                    .BindToColorA(_failOverlay)
                    .ToUniTask(ct);
            }

            // (2) パネルシェイクイン (フェード + シェイク同時)
            if (_failCanvasGroup != null && _failPanelRect != null)
            {
                var fadeHandle = LMotion.Create(0f, 1f, 0.3f)
                    .BindToAlpha(_failCanvasGroup);
                var shakeHandle = LMotion.Shake.Create(_failPanelRect.anchoredPosition, new Vector2(20f, 20f), 0.5f)
                    .WithFrequency(12)
                    .BindToAnchoredPosition(_failPanelRect);

                await UniTask.WhenAll(fadeHandle.ToUniTask(ct), shakeHandle.ToUniTask(ct));
            }
            else if (_failCanvasGroup != null)
            {
                await LMotion.Create(0f, 1f, 0.3f)
                    .BindToAlpha(_failCanvasGroup)
                    .ToUniTask(ct);
            }

            // (3) リトライボタン遅延表示
            if (_failRetryGroup != null)
            {
                await UniTask.Delay(500, cancellationToken: ct);
                await LMotion.Create(0f, 1f, 0.3f)
                    .BindToAlpha(_failRetryGroup)
                    .ToUniTask(ct);
            }
        }

        private static string FormatTime(float seconds)
        {
            var m = (int)(seconds / 60);
            var s = (int)(seconds % 60);
            return $"{m:00}:{s:00}";
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            if (_pulseHandle.IsActive()) _pulseHandle.Cancel();
        }
    }
}
