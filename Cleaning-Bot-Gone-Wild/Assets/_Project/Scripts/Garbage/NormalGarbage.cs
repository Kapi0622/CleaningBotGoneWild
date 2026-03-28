using UnityEngine;

namespace CleaningBot.Garbage
{
    /// <summary>
    /// 普通ゴミ。すべての武器で除去可能。
    /// GarbageData.hasPhysicalCollision が false の場合、コライダーを Trigger に切り替える。
    /// </summary>
    public class NormalGarbage : GarbageBase
    {
        private void Awake()
        {
            if (Data != null && !Data.hasPhysicalCollision)
            {
                var col = GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
        }
    }
}
