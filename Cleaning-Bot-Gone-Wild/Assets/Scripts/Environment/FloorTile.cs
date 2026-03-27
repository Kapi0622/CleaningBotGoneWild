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

        // MaterialPropertyBlock を使いマテリアルインスタンス化を防ぐ
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

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
            _collider.enabled = (next != TileState.Collapsed);

            if (next == TileState.Cracked)   OnCracked();
            if (next == TileState.Collapsed) OnCollapsed();

            // プロトタイプ用の色フィードバック（STEP 12 でテクスチャ/エフェクトに置き換え）
            Color color = next switch
            {
                TileState.Cracked   => new Color(1f, 0.6f, 0.2f),
                TileState.Collapsed => new Color(0.3f, 0.1f, 0.1f),
                _                   => _normalColor,
            };
            ApplyColor(color);
        }

        protected virtual void OnCracked()   => ParticlePlayer.PlayAt(_crackEffectPrefab, transform.position);

        protected virtual void OnCollapsed() => ParticlePlayer.PlayAt(_collapseEffectPrefab, transform.position);

        private void ApplyColor(Color color)
        {
            _propertyBlock.SetColor(BaseColorID, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
