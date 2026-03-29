using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 残りゴミ数の UI 表示。
    /// 減少バウンス、残り3個以下で赤⇔オレンジ点滅、全消去で拡大バースト演出。
    /// </summary>
    public class GarbageView : MonoBehaviour
    {
        private const int BlinkThreshold = 3;

        [SerializeField] private TMP_Text _garbageCountText;

        private Color _defaultColor;
        private MotionHandle _blinkHandle;
        private bool _isBlinking;
        private CancellationTokenSource _burstCts;

        private void Awake()
        {
            _defaultColor = _garbageCountText.color;
        }

        public void UpdateGarbageCount(int count)
        {
            _garbageCountText.text = $"残り {count} 個";

            // バウンス
            _garbageCountText.transform.localScale = Vector3.one;
            LMotion.Punch.Create(Vector3.one, Vector3.one * 0.25f, 0.2f)
                .WithFrequency(6)
                .BindToLocalScale(_garbageCountText.transform)
                .AddTo(this);

            // 残り3個以下で赤⇔オレンジ点滅開始
            if (count <= BlinkThreshold && count > 0 && !_isBlinking)
            {
                _isBlinking = true;
                if (_blinkHandle.IsActive()) _blinkHandle.Cancel();
                _garbageCountText.color = Color.red;
                _blinkHandle = LMotion.Create(Color.red, new Color(1f, 0.5f, 0f), 0.4f)
                    .WithLoops(-1, LoopType.Yoyo)
                    .BindToColor(_garbageCountText);
            }

            // 全消去バースト
            if (count == 0)
            {
                if (_blinkHandle.IsActive()) _blinkHandle.Cancel();
                _blinkHandle = default;
                _isBlinking = false;
                _garbageCountText.text = "COMPLETE!";
                _garbageCountText.color = Color.yellow;

                _burstCts?.Cancel();
                _burstCts?.Dispose();
                _burstCts = new CancellationTokenSource();
                PlayBurstAsync(_burstCts.Token).Forget();
            }
        }

        /// <summary>リトライ時に呼ぶ: 全アニメ停止+見た目リセット。</summary>
        public void ResetAnimation()
        {
            if (_blinkHandle.IsActive()) _blinkHandle.Cancel();
            _blinkHandle = default;
            _isBlinking = false;

            _burstCts?.Cancel();
            _burstCts?.Dispose();
            _burstCts = null;

            _garbageCountText.color = _defaultColor;
            _garbageCountText.transform.localScale = Vector3.one;
        }

        private async UniTaskVoid PlayBurstAsync(CancellationToken ct)
        {
            _garbageCountText.transform.localScale = Vector3.one;

            await LMotion.Create(Vector3.one, Vector3.one * 1.5f, 0.2f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(_garbageCountText.transform)
                .ToUniTask(ct);

            await LMotion.Create(Vector3.one * 1.5f, Vector3.one, 0.15f)
                .WithEase(Ease.InQuad)
                .BindToLocalScale(_garbageCountText.transform)
                .ToUniTask(ct);

            await LMotion.Punch.Create(Vector3.one, Vector3.one * 0.2f, 0.3f)
                .BindToLocalScale(_garbageCountText.transform)
                .ToUniTask(ct);
        }

        private void OnDestroy()
        {
            if (_blinkHandle.IsActive()) _blinkHandle.Cancel();
            _burstCts?.Cancel();
            _burstCts?.Dispose();
        }
    }
}
