using UnityEngine;

namespace CleaningBot.Data
{
    public enum GarbageType { Normal, Scatter } // Scatter は将来実装

    [CreateAssetMenu(fileName = "GarbageData", menuName = "CleaningBot/GarbageData")]
    public class GarbageData : ScriptableObject
    {
        public GarbageType garbageType;
        public int scoreValue;
        public bool hasPhysicalCollision = true;
    }
}
