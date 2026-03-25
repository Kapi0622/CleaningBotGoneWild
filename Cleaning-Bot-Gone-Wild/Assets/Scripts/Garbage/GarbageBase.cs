using R3;
using UnityEngine;
using CleaningBot.Data;

namespace CleaningBot.Garbage
{
    /// <summary>
    /// ゴミの基底クラス。
    /// Remove() 呼び出し時に OnRemoved を発火してから自身を Destroy する。
    /// GarbageRegistry が OnRemoved を購読して GarbageModel へ通知する。
    /// </summary>
    public abstract class GarbageBase : MonoBehaviour
    {
        [field: SerializeField] public GarbageData Data { get; private set; }

        public readonly Subject<GarbageBase> OnRemoved = new();

        private bool _isRemoved;

        public void Remove()
        {
            if (_isRemoved) return;
            _isRemoved = true;
            OnRemoved.OnNext(this);
            OnRemoved.Dispose();
            Destroy(gameObject);
        }
    }
}
