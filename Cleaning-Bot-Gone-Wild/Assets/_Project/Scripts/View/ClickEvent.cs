using CleaningBot.Audio;
using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CleaningBot.View
{
    /// <summary>
    /// 任意の UI GameObject にアタッチしてクリックを R3 Observable に変換する汎用コンポーネント。
    /// Unity 標準の Button に依存しないため、SE・アニメーション等の拡張が容易。
    /// Normal/Highlighted/Pressed/Disabled の色遷移は LitMotion ベース。
    /// ホバーで拡大、クリックでパンチスケール演出付き。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ClickEvent : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Color Transition")]
        [SerializeField] [Range(0f, 2f)] private float _highlightBrightness = 1.1f;
        [SerializeField] [Range(0f, 1f)] private float _pressedBrightness   = 0.78f;
        [SerializeField] [Range(0f, 1f)] private float _disabledAlpha       = 0.5f;
        [SerializeField] [Range(0f, 5f)] private float _fadeDuration        = 0.1f;

        private Color _baseColor;

        [Header("Interactable")]
        [SerializeField] private bool _interactable = true;

        [Header("Audio")]
        [SerializeField] private AudioClip _clickSound; // 未設定なら UiSoundPlayer の共通SE にフォールバック

        private Image _image;
        private UiSoundPlayer _soundPlayer;
        private MotionHandle _colorHandle;
        private MotionHandle _hoverHandle;
        private readonly Subject<Unit> _onClick = new();
        public Observable<Unit> OnClicked => _onClick;

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                ApplyColor(_interactable ? _baseColor : DisabledColor);
            }
        }

        private Color HighlightColor => new Color(
            Mathf.Clamp01(_baseColor.r * _highlightBrightness),
            Mathf.Clamp01(_baseColor.g * _highlightBrightness),
            Mathf.Clamp01(_baseColor.b * _highlightBrightness),
            _baseColor.a);

        private Color PressedColor => new Color(
            _baseColor.r * _pressedBrightness,
            _baseColor.g * _pressedBrightness,
            _baseColor.b * _pressedBrightness,
            _baseColor.a);

        private Color DisabledColor => new Color(
            _baseColor.r,
            _baseColor.g,
            _baseColor.b,
            _baseColor.a * _disabledAlpha);

        private void Awake()
        {
            _image = GetComponent<Image>();
            _baseColor = _image.color;
            _soundPlayer = GetComponentInParent<UiSoundPlayer>();
            ApplyColor(_interactable ? _baseColor : DisabledColor);
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (!_interactable) return;
            if (_clickSound != null)
                AudioSource.PlayClipAtPoint(_clickSound, transform.position);
            else
                _soundPlayer?.PlayDefaultSound();

            // クリックパンチ演出
            transform.localScale = Vector3.one;
            LMotion.Punch.Create(Vector3.one, Vector3.one * 0.1f, 0.15f)
                .BindToLocalScale(transform)
                .AddTo(this);

            _onClick.OnNext(Unit.Default);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(HighlightColor);
            if (_hoverHandle.IsActive()) _hoverHandle.Cancel();
            _hoverHandle = LMotion.Create(transform.localScale, Vector3.one * 1.05f, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_baseColor);
            if (_hoverHandle.IsActive()) _hoverHandle.Cancel();
            _hoverHandle = LMotion.Create(transform.localScale, Vector3.one, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(PressedColor);
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(HighlightColor);
        }

        private void ApplyColor(Color target)
        {
            if (_image == null) return;
            if (_colorHandle.IsActive()) _colorHandle.Cancel();
            _colorHandle = LMotion.Create(_image.color, target, _fadeDuration)
                .BindToColor(_image);
        }

        private void OnDestroy()
        {
            _onClick.Dispose();
            if (_colorHandle.IsActive()) _colorHandle.Cancel();
            if (_hoverHandle.IsActive()) _hoverHandle.Cancel();
        }
    }
}
