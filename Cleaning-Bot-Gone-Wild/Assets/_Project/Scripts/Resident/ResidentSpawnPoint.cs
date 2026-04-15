using UnityEngine;

namespace CleaningBot.Resident
{
    /// <summary>
    /// 住人のスポーン地点を表すコンポーネント。
    /// StagePrefab 内の子 GameObject に付与し、CameraDirector と同様に
    /// GetComponentsInChildren で収集して StageInitializer に渡す。
    /// residentPrefabOverride が null のとき StageData.residentPrefab（デフォルト）を使用する。
    /// </summary>
    public class ResidentSpawnPoint : MonoBehaviour
    {
        [Tooltip("null のとき StageData.residentPrefab を使用")]
        public GameObject residentPrefabOverride;

        // 将来の拡張フィールド例:
        // public ResidentSkinData skinData;
        // public ResidentBehaviorType behaviorType;
    }
}
