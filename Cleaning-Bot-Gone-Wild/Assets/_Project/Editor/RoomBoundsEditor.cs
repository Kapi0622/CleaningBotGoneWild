using UnityEditor;
using UnityEngine;
using CleaningBot.Environment;

namespace CleaningBot.Editor
{
    /// <summary>
    /// RoomBounds のコライダー境界と対応カメラ名を SceneView に描画するカスタムエディター。
    ///
    /// 表示内容:
    ///   - 部屋コライダーの床面（シアン半透明塗りつぶし）
    ///   - 部屋の 6 面の輪郭線
    ///   - 部屋名・対応 CinemachineCamera 名のラベル
    /// </summary>
    [CustomEditor(typeof(RoomBounds))]
    public class RoomBoundsEditor : UnityEditor.Editor
    {
        private static readonly Color OutlineColor = new Color(0.20f, 0.85f, 1.00f, 0.90f);
        private static readonly Color FillColor    = new Color(0.20f, 0.85f, 1.00f, 0.05f);

        // DrawSolidRectangleWithOutline に渡す配列を static で確保し、
        // 毎フレームの new[] によるヒープ割り当てを回避する。
        private static readonly Vector3[] _floorRect   = new Vector3[4];
        private static readonly Vector3[] _ceilingRect = new Vector3[4];

        private void OnSceneGUI()
        {
            var roomBounds = (RoomBounds)target;
            var col = roomBounds.GetComponent<Collider>();
            if (col == null) return;

            Bounds b = col.bounds;

            // 下面・上面・側面の 8 頂点
            Vector3 b000 = new Vector3(b.min.x, b.min.y, b.min.z);
            Vector3 b100 = new Vector3(b.max.x, b.min.y, b.min.z);
            Vector3 b110 = new Vector3(b.max.x, b.min.y, b.max.z);
            Vector3 b010 = new Vector3(b.min.x, b.min.y, b.max.z);
            Vector3 b001 = new Vector3(b.min.x, b.max.y, b.min.z);
            Vector3 b101 = new Vector3(b.max.x, b.max.y, b.min.z);
            Vector3 b111 = new Vector3(b.max.x, b.max.y, b.max.z);
            Vector3 b011 = new Vector3(b.min.x, b.max.y, b.max.z);

            // 床面（半透明塗りつぶし）
            _floorRect[0] = b000; _floorRect[1] = b100;
            _floorRect[2] = b110; _floorRect[3] = b010;
            Handles.DrawSolidRectangleWithOutline(_floorRect, FillColor, OutlineColor);

            // 天井面
            _ceilingRect[0] = b001; _ceilingRect[1] = b101;
            _ceilingRect[2] = b111; _ceilingRect[3] = b011;
            Handles.DrawSolidRectangleWithOutline(_ceilingRect, Color.clear, OutlineColor);

            // 側面の垂直辺
            Handles.color = OutlineColor;
            Handles.DrawLine(b000, b001);
            Handles.DrawLine(b100, b101);
            Handles.DrawLine(b110, b111);
            Handles.DrawLine(b010, b011);

            // 部屋名・カメラ名ラベル
            string cameraName = roomBounds.VirtualCamera != null
                ? roomBounds.VirtualCamera.name
                : "カメラ未設定";
            Handles.Label(
                b.center + Vector3.up * (b.extents.y + 0.4f),
                $"[{roomBounds.name}]\n{cameraName}");
        }
    }
}
