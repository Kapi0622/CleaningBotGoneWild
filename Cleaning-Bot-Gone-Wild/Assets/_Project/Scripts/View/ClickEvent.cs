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
        [SerializeField] private Color _normalColor    = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _highlightColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        [SerializeField] private Color _pressedColor   = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
        [SerializeField] private Color _disabledColor  = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.502f);
        [SerializeField] [Range(0f, 5f)] private float _fadeDuration = 0.1f;

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
                ApplyColor(_interactable ? _normalColor : _disabledColor);
            }
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
            _soundPlayer = GetComponentInParent<UiSoundPlayer>();
            ApplyColor(_interactable ? _normalColor : _disabledColor);
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
            ApplyColor(_highlightColor);
            if (_hoverHandle.IsActive()) _hoverHandle.Cancel();
            _hoverHandle = LMotion.Create(transform.localScale, Vector3.one * 1.05f, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_normalColor);
            if (_hoverHandle.IsActive()) _hoverHandle.Cancel();
            _hoverHandle = LMotion.Create(transform.localScale, Vector3.one, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(transform);
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_pressedColor);
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_highlightColor);
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
