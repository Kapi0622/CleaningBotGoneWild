using UnityEngine;

namespace CleaningBot.Environment
{
    /// <summary>
    /// 床グリッド全体を管理する。
    /// 唯一の外部公開窓口はApplyDamage()。WeaponStrategyはFloorTileを直接参照しない。
    /// </summary>
    public class FloorGrid : MonoBehaviour
    {
        [SerializeField] private FloorTile _tilePrefab;
        [SerializeField] private int _gridWidth = 10;
        [SerializeField] private int _gridDepth = 10;
        [SerializeField] private float _cellSize = 2f;

        private FloorTile[,] _tiles;

        private void Awake()
        {
            GenerateGrid();
        }

        private void GenerateGrid()
        {
            _tiles = new FloorTile[_gridWidth, _gridDepth];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    var pos = GridToWorld(x, z);
                    var tile = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
                    tile.name = $"Tile_{x}_{z}";
                    _tiles[x, z] = tile;
                }
            }
        }

        /// <summary>
        /// 武器が床にダメージを与える唯一の窓口。
        /// 中身はSTEP5で実装する。STEP1〜4はこのメソッドが存在するだけでOK。
        /// </summary>
        public void ApplyDamage(Vector3 hitPosition, float radius, int damageAmount)
        {
            // TODO: STEP5で実装する
            // hitPositionとradiusからグリッド座標を計算し、範囲内のFloorTileにダメージを分配する
        }

        /// <summary>
        /// StageResetterから呼ばれる。全タイルを通常状態に戻す。
        /// </summary>
        public void Reset()
        {
            foreach (var tile in _tiles)
            {
                tile.ResetState();
            }
        }

        private Vector3 GridToWorld(int x, int z)
        {
            // グリッド原点をFloorGridオブジェクトの位置に合わせる
            return transform.position + new Vector3(x * _cellSize, 0f, z * _cellSize);
        }
    }
}