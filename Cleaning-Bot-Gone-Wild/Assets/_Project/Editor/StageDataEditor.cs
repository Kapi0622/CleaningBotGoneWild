using System.IO;
using UnityEditor;
using UnityEngine;
using CleaningBot.Data;

namespace CleaningBot.Editor
{
    /// <summary>
    /// StageData の SceneView 編集を提供するカスタムインスペクター。
    ///
    /// 機能:
    ///   - ゴミスポーン位置を SceneView 上のハンドルでドラッグ移動
    ///   - ゴミ追加モード: SceneView クリックで GarbageSpawnData を追加
    ///   - 「このステージをベースに新規作成」ボタン (StageData + 環境プレハブを複製し StageDatabase に登録)
    /// </summary>
    [CustomEditor(typeof(StageData))]
    public class StageDataEditor : UnityEditor.Editor
    {
        private bool _addMode;

        // ゴミ番号ごとに色を変えて視認性を上げる
        private static readonly Color[] SpawnColors =
        {
            new Color(1.00f, 0.45f, 0.10f),
            new Color(0.20f, 0.85f, 0.25f),
            new Color(0.25f, 0.55f, 1.00f),
            new Color(0.90f, 0.20f, 0.90f),
            new Color(1.00f, 0.90f, 0.10f),
            new Color(0.10f, 0.90f, 0.90f),
        };

        private void OnEnable()  => SceneView.duringSceneGui += DrawSceneGUI;
        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneGUI;
            _addMode = false;
        }

        // ---------------------------------------------------------------
        // Inspector GUI
        // ---------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("ステージ編集ツール", EditorStyles.boldLabel);

            // ゴミ追加モードトグル
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _addMode ? new Color(0.5f, 1f, 0.5f) : Color.white;
            string btnLabel = _addMode
                ? "● ゴミ追加モード ON  (SceneView をクリックで配置)"
                : "ゴミ追加モード";
            if (GUILayout.Button(btnLabel))
            {
                _addMode = !_addMode;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = prevBg;

            EditorGUILayout.Space(4);
            if (GUILayout.Button("このステージをベースに新規ステージ作成"))
                CreateNewStageFromTemplate();
        }

        // ---------------------------------------------------------------
        // SceneGUI
        // ---------------------------------------------------------------

        private void DrawSceneGUI(SceneView _)
        {
            var stageData = (StageData)target;
            if (stageData == null || stageData.garbageSpawns == null) return;

            HandleAddMode(stageData);
            DrawSpawnHandles(stageData);
        }

        /// <summary>追加モード中: SceneView クリックでスポーンを追加する。</summary>
        private void HandleAddMode(StageData stageData)
        {
            if (!_addMode) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;

            // カーソル位置のプレビュー円を描画
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                if (TryGetGroundPoint(HandleUtility.GUIPointToWorldRay(e.mousePosition), out Vector3 hover))
                {
                    Handles.color = new Color(1f, 1f, 0.3f, 0.7f);
                    Handles.DrawWireDisc(hover, Vector3.up, 0.4f);
                    Handles.DrawLine(hover, hover + Vector3.up * 0.6f);
                }
                SceneView.RepaintAll();
            }

            // クリックでスポーンを追加
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (TryGetGroundPoint(HandleUtility.GUIPointToWorldRay(e.mousePosition), out Vector3 point))
                {
                    Undo.RecordObject(stageData, "Add Garbage Spawn");
                    stageData.garbageSpawns.Add(new GarbageSpawnData { spawnPosition = point });
                    EditorUtility.SetDirty(stageData);
                    e.Use();
                    Repaint();
                }
            }
        }

        /// <summary>各ゴミスポーンのハンドル・ラベルを描画する。</summary>
        private void DrawSpawnHandles(StageData stageData)
        {
            for (int i = 0; i < stageData.garbageSpawns.Count; i++)
            {
                var   spawn = stageData.garbageSpawns[i];
                Color col   = SpawnColors[i % SpawnColors.Length];

                float size = HandleUtility.GetHandleSize(spawn.spawnPosition) * 0.15f;

                // ドラッグハンドル
                Handles.color = col;
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(
                    spawn.spawnPosition,
                    size,
                    Vector3.zero,
                    Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(stageData, "Move Garbage Spawn");
                    // Y 座標は維持し XZ 平面のみ移動
                    spawn.spawnPosition = new Vector3(newPos.x, spawn.spawnPosition.y, newPos.z);
                    EditorUtility.SetDirty(stageData);
                }

                // 地面の足元円
                Handles.color = new Color(col.r, col.g, col.b, 0.35f);
                Handles.DrawWireDisc(
                    new Vector3(spawn.spawnPosition.x, 0f, spawn.spawnPosition.z),
                    Vector3.up, size * 1.8f);

                // ラベル
                string prefabName = spawn.prefab != null ? spawn.prefab.name : "未設定";
                Handles.color = Color.white;
                Handles.Label(spawn.spawnPosition + Vector3.up * (size + 0.35f), $"#{i} {prefabName}");
            }
        }

        // ---------------------------------------------------------------
        // 新規ステージ作成
        // ---------------------------------------------------------------

        private void CreateNewStageFromTemplate()
        {
            var    stageData   = (StageData)target;
            string sourcePath  = AssetDatabase.GetAssetPath(stageData);
            string dir         = Path.GetDirectoryName(sourcePath);
            int    newId       = stageData.stageId + 1;

            // --- StageData を複製 ---
            string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{dir}/StageData_Stage{newId}.asset");
            AssetDatabase.CopyAsset(sourcePath, newAssetPath);
            AssetDatabase.SaveAssets();

            var newStage = AssetDatabase.LoadAssetAtPath<StageData>(newAssetPath);
            newStage.stageId     = newId;
            newStage.stageName   = $"Stage{newId}";
            newStage.displayName = $"ステージ {newId}";

            // --- 環境プレハブを複製 ---
            if (stageData.stageEnvironmentPrefab != null)
            {
                string prefabSrc  = AssetDatabase.GetAssetPath(stageData.stageEnvironmentPrefab);
                string prefabDir  = Path.GetDirectoryName(prefabSrc);
                string newPrefab  = AssetDatabase.GenerateUniqueAssetPath(
                    $"{prefabDir}/StageEnvironment_Stage{newId}.prefab");

                AssetDatabase.CopyAsset(prefabSrc, newPrefab);
                AssetDatabase.SaveAssets();

                newStage.stageEnvironmentPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(newPrefab);
            }

            EditorUtility.SetDirty(newStage);
            AssetDatabase.SaveAssets();

            // --- StageDatabase が1つだけ存在する場合に自動登録 ---
            string[] dbGuids = AssetDatabase.FindAssets("t:StageDatabase");
            if (dbGuids.Length == 1)
            {
                string       dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
                StageDatabase db    = AssetDatabase.LoadAssetAtPath<StageDatabase>(dbPath);
                if (db != null && !db.stages.Contains(newStage))
                {
                    Undo.RecordObject(db, "Register New Stage");
                    db.stages.Add(newStage);
                    EditorUtility.SetDirty(db);
                    AssetDatabase.SaveAssets();
                }
            }
            else if (dbGuids.Length > 1)
            {
                Debug.LogWarning("[StageDataEditor] StageDatabase が複数存在するため、自動登録をスキップしました。手動で登録してください。");
            }

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newStage;
            EditorGUIUtility.PingObject(newStage);
            Debug.Log($"[StageDataEditor] Stage {newId} を作成しました: {newAssetPath}");
        }

        // ---------------------------------------------------------------
        // ユーティリティ
        // ---------------------------------------------------------------

        /// <summary>Ray と Y=0 平面の交点を求める。</summary>
        private static bool TryGetGroundPoint(Ray ray, out Vector3 point)
        {
            if (Mathf.Abs(ray.direction.y) < 1e-5f) { point = Vector3.zero; return false; }
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) { point = Vector3.zero; return false; }
            point = ray.origin + ray.direction * t;
            return true;
        }
    }
}
