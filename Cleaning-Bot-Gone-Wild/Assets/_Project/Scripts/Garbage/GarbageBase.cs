using CleaningBot.Effects;
using CleaningBot.Data;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CleaningBot.Garbage
{
    /// <summary>
    /// ゴミの基底クラス。
    /// Remove() 呼び出し時に OnRemoved を発火してから自身を Destroy する。
    /// GarbageRegistry が OnRemoved を購読して GarbageModel へ通知する。
    /// </summary>
    public abstract class GarbageBase : MonoBehaviour
    {
        [field: SerializeField] public GarbageData Data { get; private set; }

        [Header("Effects")]
        [SerializeField] private ParticleSystem _removalEffectPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip _removalSound;

        [Header("HP Bar")]
        [SerializeField] private GameObject _hpBarPrefab;

        public readonly Subject<GarbageBase> OnRemoved = new();

        private bool _isRemoved;
        private int _currentHp;
        private GameObject _hpBarInstance;
        private Image _hpFillImage;

        protected virtual void Awake()
        {
            _currentHp = Data != null ? Data.maxHp : 1;

            if (_hpBarPrefab != null)
            {
                _hpBarInstance = Instantiate(_hpBarPrefab, transform);
                _hpBarInstance.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                var fill = _hpBarInstance.transform.Find("Fill");
                if (fill != null) _hpFillImage = fill.GetComponent<Image>();
                _hpBarInstance.SetActive(false);
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isRemoved) return;
            _currentHp -= amount;

            if (_hpBarInstance != null)
            {
                _hpBarInstance.SetActive(true);
                if (_hpFillImage != null)
                {
                    float denom = Data != null ? Mathf.Max(Data.maxHp, 1) : 1f;
                    _hpFillImage.fillAmount = Mathf.Clamp01(Mathf.Max(_currentHp, 0f) / denom);
                }
            }

            if (_currentHp <= 0)
                Remove();
        }

        public void Remove()
        {
            if (_isRemoved) return;
            _isRemoved = true;
            OnRemovalEffect();
            OnRemoved.OnNext(this);
            OnRemoved.Dispose();
            Destroy(gameObject);
        }

        protected virtual void OnRemovalEffect()
        {
            ParticlePlayer.PlayAt(_removalEffectPrefab, transform.position);
            if (_removalSound != null)
                AudioSource.PlayClipAtPoint(_removalSound, transform.position);
        }
    }
}
