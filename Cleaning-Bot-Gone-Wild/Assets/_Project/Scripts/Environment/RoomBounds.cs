using R3;
using Unity.Cinemachine;
using UnityEngine;

namespace CleaningBot.Environment
{
    /// <summary>
    /// 部屋の入室トリガーを検出し、R3 Subject でイベントを発火する MonoBehaviour。
    /// Collider は必ず IsTrigger=true であること。エディタ時は自動強制、ランタイムでも検証する。
    /// ビジネスロジックは持たない。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomBounds : MonoBehaviour
    {
        [field: SerializeField]
        public CinemachineCamera VirtualCamera { get; private set; }

        private readonly Subject<RoomBounds> _onPlayerEntered = new();
        public Observable<RoomBounds> OnPlayerEntered => _onPlayerEntered;

        private void Awake()
        {
            // ビルド時も含めて isTrigger を検証する
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
                Debug.LogError($"[RoomBounds] {name} の Collider.isTrigger が false です。OnTriggerEnter が動作しません。", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _onPlayerEntered.OnNext(this);
        }

        private void OnDestroy()
        {
            _onPlayerEntered.Dispose();
        }

#if UNITY_EDITOR
        // コンポーネント初回追加時に isTrigger を自動で true にする
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        // Inspector 上で値が変更されるたびに isTrigger を強制する
        private void OnValidate()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[RoomBounds] {name} の Collider.isTrigger を強制的に true に設定しました。", this);
            }
        }
#endif
    }
}
