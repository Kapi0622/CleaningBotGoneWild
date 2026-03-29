using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 残り時間の UI 表示。MM:SS 形式でフォーマットする。
    /// char[] バッファ + SetCharArray で GC Alloc = 0。
    /// 残り10秒: パルス + 赤色化。残り5秒: シェイク追加。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TimerView : MonoBehaviour
    {
        private const float CriticalThreshold = 5f;

        [SerializeField] private TMP_Text _timerText;

        [Header("Audio")]
        [SerializeField] private AudioClip _warningSound;

        private AudioSource _audioSource;
        private RectTransform _timerRect;
        private Vector2 _defaultAnchoredPos;
        private Color _defaultColor;
        private MotionHandle _pulseTween;
        private MotionHandle _shakeTween;
        private MotionHandle _colorTween;
        private bool _isWarningActive;
        private bool _isCriticalActive;

        // "00:00" 固定長バッファ — GCゼロ
        private readonly char[] _timeBuffer = { '0', '0', ':', '0', '0' };

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _timerRect = _timerText.GetComponent<RectTransform>();
            _defaultAnchoredPos = _timerRect.anchoredPosition;
            _defaultColor = _timerText.color;
        }

        public void UpdateTimer(float remainingSeconds)
        {
            var m = (int)(remainingSeconds / 60);
            var s = (int)(remainingSeconds % 60);
            _timeBuffer[0] = (char)('0' + m / 10);
            _timeBuffer[1] = (char)('0' + m % 10);
            // [2] は ':' 固定
            _timeBuffer[3] = (char)('0' + s / 10);
            _timeBuffer[4] = (char)('0' + s % 10);
            _timerText.SetCharArray(_timeBuffer, 0, 5);

            // 残り5秒シェイク
            if (remainingSeconds <= CriticalThreshold && !_isCriticalActive)
            {
                _isCriticalActive = true;
                if (_shakeTween.IsActive()) _shakeTween.Cancel();
                _shakeTween = LMotion.Shake.Create(_timerRect.anchoredPosition, new Vector2(3f, 3f), 0.5f)
                    .WithFrequency(14)
                    .WithLoops(-1)
                    .BindToAnchoredPosition(_timerRect);
            }
        }

        public void SetWarning(bool isLow)
        {
            if (isLow && !_isWarningActive)
            {
                _isWarningActive = true;

                // 警告音
                if (_warningSound != null)
                    _audioSource.PlayOneShot(_warningSound);

                // 赤色化
                if (_colorTween.IsActive()) _colorTween.Cancel();
                _colorTween = LMotion.Create(_timerText.color, Color.red, 0.3f)
                    .BindToColor(_timerText);

                // パルス (1⇔1.15)
                if (_pulseTween.IsActive()) _pulseTween.Cancel();
                _timerText.transform.localScale = Vector3.one;
                _pulseTween = LMotion.Create(1f, 1.15f, 0.3f)
                    .WithLoops(-1, LoopType.Yoyo)
                    .WithEase(Ease.InOutSine)
                    .BindToLocalScaleXYZ(_timerText.transform);
            }
            else if (!isLow && _isWarningActive)
            {
                ResetWarning();
            }
        }

        /// <summary>リトライ時に呼ぶ: 全アニメ停止+見た目リセット。</summary>
        public void ResetAnimation()
        {
            ResetWarning();
        }

        private void ResetWarning()
        {
            _isWarningActive = false;
            _isCriticalActive = false;

            if (_pulseTween.IsActive()) _pulseTween.Cancel();
            _pulseTween = default;
            if (_shakeTween.IsActive()) _shakeTween.Cancel();
            _shakeTween = default;
            if (_colorTween.IsActive()) _colorTween.Cancel();
            _colorTween = default;

            _timerText.color = _defaultColor;
            _timerText.transform.localScale = Vector3.one;
            _timerRect.anchoredPosition = _defaultAnchoredPos;
        }

        private void OnDestroy()
        {
            if (_pulseTween.IsActive()) _pulseTween.Cancel();
            if (_shakeTween.IsActive()) _shakeTween.Cancel();
            if (_colorTween.IsActive()) _colorTween.Cancel();
        }
    }
}
