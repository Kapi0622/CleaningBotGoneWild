using System.Threading;
using CleaningBot.Data;
using CleaningBot.Garbage;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// 掃除機ストラテジー。
    /// クールタイムなし。向いた方向の短射程内にあるゴミを即時吸引・除去する。
    /// </summary>
    public class VacuumStrategy : IWeaponStrategy
    {
        private const float Range = 3f;
        private static readonly int GarbageLayer = LayerMask.GetMask("Garbage");

        private readonly WeaponData _data;
        private readonly Transform _origin;
        private readonly AudioSource _audioSource;

        public VacuumStrategy(WeaponData data, Transform origin, AudioSource audioSource)
        {
            _data = data;
            _origin = origin;
            _audioSource = audioSource;
        }

        public void OnEquip() { }
        public void OnUnequip() { }

        public bool CanExecute() => true;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            var hits = Physics.OverlapSphere(_origin.position, Range, GarbageLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<GarbageBase>(out var garbage))
                {
                    garbage.Remove();
                }
            }

            if (_data.fireSound != null)
            {
                _audioSource.PlayOneShot(_data.fireSound);
            }
        }
    }
}
