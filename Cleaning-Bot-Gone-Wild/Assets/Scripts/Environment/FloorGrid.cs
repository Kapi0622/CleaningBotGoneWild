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
        /// hitPosition と radius から XZ 平面での範囲内タイルを特定し、各タイルに damageAmount を与える。
        /// </summary>
        public void ApplyDamage(Vector3 hitPosition, float radius, int damageAmount)
        {
            int centerX = Mathf.RoundToInt((hitPosition.x - transform.position.x) / _cellSize);
            int centerZ = Mathf.RoundToInt((hitPosition.z - transform.position.z) / _cellSize);
            int radiusInCells = Mathf.CeilToInt(radius / _cellSize);
            float radiusSqr = radius * radius;

            for (int x = centerX - radiusInCells; x <= centerX + radiusInCells; x++)
            {
                for (int z = centerZ - radiusInCells; z <= centerZ + radiusInCells; z++)
                {
                    if (x < 0 || x >= _gridWidth || z < 0 || z >= _gridDepth) continue;

                    // XZ平面での距離チェック（Y軸の高さ差を無視）。sqrt を避けるため二乗比較
                    Vector3 tileWorldPos = GridToWorld(x, z);
                    float dx = hitPosition.x - tileWorldPos.x;
                    float dz = hitPosition.z - tileWorldPos.z;
                    float distSqr = dx * dx + dz * dz;

                    if (distSqr <= radiusSqr)
                    {
                        _tiles[x, z].TakeDamage(damageAmount);
                    }
                }
            }
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