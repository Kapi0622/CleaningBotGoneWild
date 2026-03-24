using UnityEngine;

namespace CleaningBot.Garbage
{
    /// <summary>
    /// STEP 3: ゴミの最小実装。Remove() で即 Destroy する。
    /// STEP 4 で R3 Subject・GarbageRegistry 連携に拡張する。
    /// </summary>
    public class GarbageBase : MonoBehaviour
    {
        public void Remove()
        {
            Destroy(gameObject);
        }
    }
}
