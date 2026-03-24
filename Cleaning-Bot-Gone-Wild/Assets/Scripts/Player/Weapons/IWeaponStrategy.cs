using System.Threading;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    public interface IWeaponStrategy
    {
        void OnEquip();
        void OnUnequip();
        void Execute(Vector3 direction, CancellationToken ct);
        bool CanExecute();
    }
}
