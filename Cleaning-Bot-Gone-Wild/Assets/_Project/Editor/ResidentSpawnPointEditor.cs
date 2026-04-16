using UnityEditor;
using UnityEngine;
using CleaningBot.Resident;

namespace CleaningBot.Editor
{
    /// <summary>
    /// ResidentSpawnPoint の向きと使用プレハブを SceneView に描画するカスタムエディター。
    ///
    /// 表示内容:
    ///   - スポーン方向を示す矢印
    ///   - 足元の黄色円
    ///   - 使用プレハブ名（オーバーライドがなければ「デフォルト住人」）
    /// </summary>
    [CustomEditor(typeof(ResidentSpawnPoint))]
    public class ResidentSpawnPointEditor : UnityEditor.Editor
    {
        private static readonly Color SpawnColor = new Color(1.00f, 0.78f, 0.18f, 0.90f);

        private void OnSceneGUI()
        {
            var point = (ResidentSpawnPoint)target;
            Vector3 pos = point.transform.position;
            Vector3 fwd = point.transform.forward;

            Handles.color = SpawnColor;

            // 方向矢印
            float arrowSize = HandleUtility.GetHandleSize(pos) * 0.7f;
            Handles.ArrowHandleCap(
                0, pos, Quaternion.LookRotation(fwd), arrowSize, EventType.Repaint);

            // 足元の円
            Handles.DrawWireDisc(pos, Vector3.up, 0.5f);

            // ラベル
            string prefabInfo = point.residentPrefabOverride != null
                ? point.residentPrefabOverride.name
                : "デフォルト住人";
            Handles.Label(pos + Vector3.up * 1.6f, $"住人スポーン\n{prefabInfo}");
        }
    }
}
