using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CleaningBot.View
{
    /// <summary>
    /// 任意の UI GameObject にアタッチしてクリックを R3 Observable に変換する汎用コンポーネント。
    /// Unity 標準の Button に依存しないため、SE・アニメーション等の拡張が容易。
    /// Normal/Highlighted/Pressed/Disabled の色遷移は標準 Button と同等。
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

        private Image _image;
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
            ApplyColor(_interactable ? _normalColor : _disabledColor);
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (!_interactable) return;
            _onClick.OnNext(Unit.Default);
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_highlightColor);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_interactable) return;
            ApplyColor(_normalColor);
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
            _image.CrossFadeColor(target, _fadeDuration, true, true);
        }

        private void OnDestroy() => _onClick.Dispose();
    }
}
