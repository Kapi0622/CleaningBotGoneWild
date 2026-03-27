using CleaningBot.Effects;
using CleaningBot.Data;
using R3;
using UnityEngine;

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

        [Header("Effects")]
        [SerializeField] private ParticleSystem _removalEffectPrefab;

        public readonly Subject<GarbageBase> OnRemoved = new();

        private bool _isRemoved;

        public void Remove()
        {
            if (_isRemoved) return;
            _isRemoved = true;
            OnRemovalEffect();
            OnRemoved.OnNext(this);
            OnRemoved.Dispose();
            Destroy(gameObject);
        }

        protected virtual void OnRemovalEffect() => ParticlePlayer.PlayAt(_removalEffectPrefab, transform.position);
    }
}
