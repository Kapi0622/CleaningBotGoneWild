using R3;
using Unity.Cinemachine;
using UnityEngine;

namespace CleaningBot.Environment
{
    /// <summary>
    /// 部屋の入室トリガーを検出し、R3 Subject でイベントを発火する MonoBehaviour。
    /// Collider は IsTrigger=true の Box Collider を想定。
    /// ビジネスロジックは持たない。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomBounds : MonoBehaviour
    {
        [field: SerializeField]
        public CinemachineCamera VirtualCamera { get; private set; }

        private readonly Subject<RoomBounds> _onPlayerEntered = new();
        public Observable<RoomBounds> OnPlayerEntered => _onPlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _onPlayerEntered.OnNext(this);
        }

        private void OnDestroy()
        {
            _onPlayerEntered.Dispose();
        }
    }
}
