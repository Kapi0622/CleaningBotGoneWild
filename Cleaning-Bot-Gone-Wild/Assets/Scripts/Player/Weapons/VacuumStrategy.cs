using System.Threading;
using CleaningBot.Data;
using CleaningBot.Garbage;
using CleaningBot.Resident;
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
        // 前方 90° の半球コーン（cos90° = 0）。背後のゴミは除去しない
        private const float HalfConeCos = 0f;
        private static readonly int GarbageLayer  = LayerMask.GetMask("Garbage");
        private static readonly int ResidentLayer = LayerMask.GetMask("Resident");

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
                // 原点からターゲットへの方向と FacingDirection の内積で前方コーン判定
                var toTarget = (hit.transform.position - _origin.position).normalized;
                if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;

                if (hit.TryGetComponent<GarbageBase>(out var garbage))
                {
                    garbage.Remove();
                }
            }

            // 同じ前方コーン内にいる住人に怒り状態をトリガーする
            var residentHits = Physics.OverlapSphere(_origin.position, Range, ResidentLayer);
            foreach (var hit in residentHits)
            {
                var toTarget = (hit.transform.position - _origin.position).normalized;
                if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;
                if (hit.TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.TriggerAngry();
            }

            if (_data.fireSound != null)
            {
                _audioSource.PlayOneShot(_data.fireSound);
            }
        }
    }
}
