using UnityEngine;

namespace CleaningBot.Environment
{
    public enum TileState { Normal, Cracked, Collapsed }

    /// <summary>
    /// 床の1セルを管理する。
    /// 状態遷移ロジックはSTEP5で追記する。
    /// </summary>
    public class FloorTile : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 3;

        private int _currentHp;
        private TileState _state = TileState.Normal;
        private Collider _collider;

        private void Awake()
        {
            _currentHp = _maxHp;
            _collider = GetComponent<Collider>();
        }

        /// <summary>
        /// FloorGridのApplyDamage()からのみ呼ばれる。
        /// </summary>
        public void TakeDamage(int amount)
        {
            // TODO: STEP5で実装する
        }

        /// <summary>
        /// StageResetter → FloorGrid.Reset() → ここに到達する。
        /// </summary>
        public void ResetState()
        {
            _currentHp = _maxHp;
            _state = TileState.Normal;
            if (_collider != null) _collider.enabled = true;
            // TODO: STEP5でビジュアルのリセットも追加する
        }
    }
}