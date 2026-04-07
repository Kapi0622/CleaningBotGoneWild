using System;
using System.Collections.Generic;
using System.Linq;
using CleaningBot.Environment;
using R3;

namespace CleaningBot.CameraSystem
{
    /// <summary>
    /// 部屋入室イベントを購読し、対応する CinemachineCamera の enabled を切り替える純粋クラス。
    /// CinemachineBrain が enabled=true の VCam を自動的にブレンド対象にする。
    /// </summary>
    public class CameraDirector : IDisposable
    {
        private readonly IReadOnlyList<RoomBounds> _rooms;
        private RoomBounds _currentRoom;
        private IDisposable _subscription;

        public CameraDirector(IReadOnlyList<RoomBounds> rooms)
        {
            _rooms = rooms;
        }

        /// <summary>
        /// 全部屋の OnPlayerEntered を購読する。
        /// </summary>
        public void Initialize(RoomBounds startRoom)
        {
            // 起動時は startRoom の VCam のみ有効化
            foreach (var room in _rooms)
            {
                if (room.VirtualCamera != null)
                    room.VirtualCamera.enabled = (room == startRoom);
            }
            _currentRoom = startRoom;

            // 全部屋の入室イベントを1本のストリームにまとめて購読
            _subscription = Observable
                .Merge(_rooms.Select(r => r.OnPlayerEntered))
                .Subscribe(OnRoomEntered);
        }

        private void OnRoomEntered(RoomBounds enteredRoom)
        {
            if (enteredRoom == _currentRoom) return;

            if (_currentRoom?.VirtualCamera != null)
                _currentRoom.VirtualCamera.enabled = false;

            if (enteredRoom.VirtualCamera != null)
                enteredRoom.VirtualCamera.enabled = true;

            _currentRoom = enteredRoom;
        }

        /// <summary>
        /// StageResetter から呼ばれる。カメラを初期部屋にリセットする。
        /// 画面が黒い間に呼ぶことでプレイヤーにカットが見えない。
        /// </summary>
        public void ResetToDefault(RoomBounds startRoom)
        {
            foreach (var room in _rooms)
            {
                if (room.VirtualCamera != null)
                    room.VirtualCamera.enabled = (room == startRoom);
            }
            _currentRoom = startRoom;
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
