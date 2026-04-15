using UnityEditor;
using UnityEngine;
using CleaningBot.Environment;

namespace CleaningBot.Editor
{
    /// <summary>
    /// FloorGrid の床面積・グリッド線を SceneView に描画するカスタムエディター。
    ///
    /// 表示内容:
    ///   - 床全体の外枠（緑の半透明塗りつぶし + 実線）
    ///   - 個々のセル区切り線（セル数が 400 以下の場合）
    ///   - グリッドサイズ情報のラベル
    /// </summary>
    [CustomEditor(typeof(FloorGrid))]
    public class FloorGridEditor : UnityEditor.Editor
    {
        private static readonly Color OutlineColor = new Color(0.30f, 0.80f, 0.30f, 0.85f);
        private static readonly Color FillColor    = new Color(0.30f, 0.80f, 0.30f, 0.07f);
        private static readonly Color LineColor    = new Color(0.30f, 0.80f, 0.30f, 0.22f);

        private void OnSceneGUI()
        {
            var grid = (FloorGrid)target;
            var so   = serializedObject;
            so.Update();

            int   gridWidth = so.FindProperty("_gridWidth").intValue;
            int   gridDepth = so.FindProperty("_gridDepth").intValue;
            float cellSize  = so.FindProperty("_cellSize").floatValue;

            if (gridWidth <= 0 || gridDepth <= 0 || cellSize <= 0f) return;

            Vector3 origin = grid.transform.position;

            // タイル配置: tile(x,z) の中心 = origin + (x*cellSize, 0, z*cellSize)
            // 視覚的フットプリント: 各タイルが cellSize 四方を占める想定
            float halfCell = cellSize * 0.5f;
            float totalW   = gridWidth  * cellSize;
            float totalD   = gridDepth  * cellSize;

            Vector3 c0 = new Vector3(origin.x - halfCell,          origin.y, origin.z - halfCell);
            Vector3 c1 = new Vector3(origin.x + totalW - halfCell, origin.y, origin.z - halfCell);
            Vector3 c2 = new Vector3(origin.x + totalW - halfCell, origin.y, origin.z + totalD - halfCell);
            Vector3 c3 = new Vector3(origin.x - halfCell,          origin.y, origin.z + totalD - halfCell);

            // 床面の塗りつぶし + 外枠
            Handles.DrawSolidRectangleWithOutline(new[] { c0, c1, c2, c3 }, FillColor, OutlineColor);

            // セル区切り線 (400 セル以下の場合)
            if (gridWidth * gridDepth <= 400)
            {
                Handles.color = LineColor;
                for (int x = 0; x <= gridWidth; x++)
                {
                    float px = origin.x - halfCell + x * cellSize;
                    Handles.DrawLine(
                        new Vector3(px, origin.y, origin.z - halfCell),
                        new Vector3(px, origin.y, origin.z + totalD - halfCell));
                }
                for (int z = 0; z <= gridDepth; z++)
                {
                    float pz = origin.z - halfCell + z * cellSize;
                    Handles.DrawLine(
                        new Vector3(origin.x - halfCell,          origin.y, pz),
                        new Vector3(origin.x + totalW - halfCell, origin.y, pz));
                }
            }

            // サイズ情報ラベル
            Vector3 labelPos = new Vector3(
                origin.x + (totalW - cellSize) * 0.5f,
                origin.y + 0.1f,
                origin.z - halfCell - 0.5f);
            Handles.color = OutlineColor;
            Handles.Label(labelPos, $"FloorGrid  {gridWidth} × {gridDepth}  (cell: {cellSize}m)");
        }
    }
}
