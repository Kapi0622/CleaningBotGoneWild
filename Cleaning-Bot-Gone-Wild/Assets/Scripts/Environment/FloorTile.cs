using UnityEngine;

namespace CleaningBot.Environment
{
    public enum TileState { Normal, Cracked, Collapsed }

    /// <summary>
    /// 床の1セルを管理する。HP に応じて Normal → Cracked → Collapsed の3段階で状態遷移する。
    /// </summary>
    public class FloorTile : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 3;

        private int _currentHp;
        private TileState _state = TileState.Normal;
        private Collider _collider;
        private MeshRenderer _renderer;
        private Color _normalColor;

        private void Awake()
        {
            _currentHp = _maxHp;
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null) _normalColor = _renderer.material.color;
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
            if (_collider != null) _collider.enabled = true;
            if (_renderer != null) _renderer.material.color = _normalColor;
        }

        private void UpdateState()
        {
            if (_currentHp <= 0)                 SetState(TileState.Collapsed);
            else if (_currentHp <= _maxHp / 2)   SetState(TileState.Cracked);
        }

        private void SetState(TileState next)
        {
            if (next == _state) return;
            _state = next;
            _collider.enabled = (next != TileState.Collapsed);

            // プロトタイプ用の色フィードバック（STEP 12 でテクスチャ/エフェクトに置き換え）
            switch (next)
            {
                case TileState.Normal:
                    _renderer.material.color = _normalColor;
                    break;
                case TileState.Cracked:
                    _renderer.material.color = new Color(1f, 0.6f, 0.2f);
                    break;
                case TileState.Collapsed:
                    _renderer.material.color = new Color(0.3f, 0.1f, 0.1f);
                    break;
            }
        }
    }
}