using System;
using CleaningBot.Effects;
using UnityEngine;

namespace CleaningBot.Environment
{
    public enum TileState { Normal, Cracked, Collapsed }

    /// <summary>
    /// 床の1セルを管理する。HP に応じて Normal → Cracked → Collapsed の3段階で状態遷移する。
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(MeshRenderer))]
    public class FloorTile : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 2;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _crackEffectPrefab;
        [SerializeField] private ParticleSystem _collapseEffectPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip _crackSound;
        [SerializeField] private AudioClip _collapseSound;

        // MaterialPropertyBlock を使いマテリアルインスタンス化を防ぐ
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        /// <summary>タイルが Collapsed 状態に遷移したとき発火する。FloorGrid が購読して SubScore を加算する。</summary>
        public event Action OnCollapsedEvent;

        /// <summary>BonusGarbageSpawner がスポーン位置の選定に使う。</summary>
        public bool IsCollapsed => _state == TileState.Collapsed;

        private int _currentHp;
        private TileState _state = TileState.Normal;
        private Collider _collider;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Color _normalColor;

        private void Awake()
        {
            _currentHp = _maxHp;
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            // sharedMaterial でベースカラーを取得（material アクセスによるインスタンス化を避ける）
            _normalColor = _renderer.sharedMaterial != null
                ? _renderer.sharedMaterial.GetColor(BaseColorID)
                : Color.white;
        }

        /// <summary>
        /// FloorGridのApplyDamage()からのみ呼ばれる。
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_state == TileState.Collapsed) return;
            _currentHp -= amount;
            UpdateState();
        }

        /// <summary>
        /// StageResetter → FloorGrid.Reset() → ここに到達する。
        /// </summary>
        public void ResetState()
        {
            _currentHp = _maxHp;
            _state = TileState.Normal;
            _collider.enabled = true;
            _renderer.enabled = true;
            ApplyColor(_normalColor);
        }

        private void UpdateState()
        {
            if (_currentHp <= 0)               SetState(TileState.Collapsed);
            else if (_currentHp <= _maxHp / 2) SetState(TileState.Cracked);
        }

        private void SetState(TileState next)
        {
            if (next == _state) return;
            _state = next;

            if (next == TileState.Cracked)
            {
                OnCracked();
                ApplyColor(new Color(1f, 0.6f, 0.2f));
            }
            else if (next == TileState.Collapsed)
            {
                _collider.enabled = false;
                _renderer.enabled = false;
                OnCollapsed();
                OnCollapsedEvent?.Invoke();
            }
        }

        protected virtual void OnCracked()
        {
            ParticlePlayer.PlayAt(_crackEffectPrefab, transform.position);
            if (_crackSound != null)
                AudioSource.PlayClipAtPoint(_crackSound, transform.position);
        }

        protected virtual void OnCollapsed()
        {
            ParticlePlayer.PlayAt(_collapseEffectPrefab, transform.position);
            if (_collapseSound != null)
                AudioSource.PlayClipAtPoint(_collapseSound, transform.position);
        }

        private void ApplyColor(Color color)
        {
            _propertyBlock.SetColor(BaseColorID, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
