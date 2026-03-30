using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CleaningBot.Editor
{
    /// <summary>
    /// バキューム射程インジケーター用の半円形 Prefab を生成するエディターユーティリティ。
    /// メニュー: CleaningBot > Create Vacuum Range Indicator Prefab
    /// 生成した Prefab を VacuumData の rangeIndicatorPrefab にアサインすること。
    /// </summary>
    public static class VacuumRangeIndicatorCreator
    {
        private const string PrefabPath   = "Assets/_Project/Prefabs/VacuumRangeIndicator.prefab";
        private const string MaterialPath = "Assets/_Project/Material/VacuumRangeIndicator.mat";

        [MenuItem("CleaningBot/Create Vacuum Range Indicator Prefab")]
        private static void CreatePrefab()
        {
            // ---- Material ----
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[VacuumRangeIndicatorCreator] URP/Lit シェーダーが見つかりません。");
                return;
            }

            var mat = new Material(shader) { name = "VacuumRangeIndicator" };
            mat.SetFloat("_Surface",   1f);   // 1 = Transparent
            mat.SetFloat("_Blend",     0f);   // 0 = Alpha
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_ZWrite",    0f);
            mat.SetColor("_BaseColor", new Color(0.3f, 0.8f, 1f, 0.35f));
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;

            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
                AssetDatabase.DeleteAsset(MaterialPath);
            AssetDatabase.CreateAsset(mat, MaterialPath);

            // ---- Step 1: Prefab をメッシュなしで先に保存 ----
            var go = new GameObject("VacuumRangeIndicator");
            go.AddComponent<MeshFilter>();                          // mesh は後で代入
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial     = mat;
            mr.shadowCastingMode  = ShadowCastingMode.Off;
            mr.receiveShadows     = false;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            // ---- Step 2: Mesh をサブアセットとして Prefab ファイルに埋め込む ----
            var mesh = CreateHalfDiscMesh(40);
            mesh.name = "HalfDiscMesh";
            AssetDatabase.AddObjectToAsset(mesh, PrefabPath);
            AssetDatabase.SaveAssets();

            // ---- Step 3: EditPrefabContentsScope で MeshFilter に mesh を代入 ----
            using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
            {
                var mf = scope.prefabContentsRoot.GetComponent<MeshFilter>();
                mf.sharedMesh = mesh;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[VacuumRangeIndicatorCreator] Prefab を生成しました: {PrefabPath}");
        }

        /// <summary>
        /// 前方半円形（-90°〜+90°、XZ 平面）の Mesh を生成する。
        /// radius = 1 (単位スケール)。VacuumStrategy 側で blastRadius に合わせてスケールする。
        /// </summary>
        private static Mesh CreateHalfDiscMesh(int segments)
        {
            int vertCount = segments + 2; // center + arc points
            var vertices  = new Vector3[vertCount];
            var uvs       = new Vector2[vertCount];
            var triangles = new int[segments * 3];

            // 中心点
            vertices[0] = Vector3.zero;
            uvs[0]      = new Vector2(0.5f, 0.5f);

            // 弧: -90°〜+90° (XZ 平面, +Z が前方)
            for (int i = 0; i <= segments; i++)
            {
                float t     = (float)i / segments;
                float angle = Mathf.Lerp(-90f, 90f, t) * Mathf.Deg2Rad;
                float x     = Mathf.Sin(angle);
                float z     = Mathf.Cos(angle);
                vertices[i + 1] = new Vector3(x, 0f, z);
                uvs[i + 1]      = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
            }

            // 三角形（扇形）
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            var mesh = new Mesh();
            mesh.vertices  = vertices;
            mesh.uv        = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
