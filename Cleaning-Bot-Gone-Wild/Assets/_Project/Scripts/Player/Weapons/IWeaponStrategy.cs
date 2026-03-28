using System.Threading;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    public interface IWeaponStrategy
    {
        void OnEquip();
        void OnUnequip();
        void Execute(Vector3 direction, CancellationToken ct);
        /// <summary>使用ボタンが離された瞬間に呼ばれる。ループエフェクトの停止などに使用する。</summary>
        void OnExecuteEnd();
        bool CanExecute();
    }
}
