using System.Threading;
using CleaningBot.Data;
using CleaningBot.Effects;
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

        // OverlapSphereNonAlloc 用バッファ
        private readonly Collider[] _garbageBuffer  = new Collider[32];
        private readonly Collider[] _residentBuffer = new Collider[16];

        private readonly WeaponData _data;
        private readonly Transform _origin;
        private readonly AudioSource _audioSource;

        private GameObject _activeEffect;

        public VacuumStrategy(WeaponData data, Transform origin, AudioSource audioSource)
        {
            _data = data;
            _origin = origin;
            _audioSource = audioSource;
        }

        public void OnEquip() { }

        public void OnUnequip()
        {
            StopEffect();
        }

        public void OnExecuteEnd()
        {
            StopEffect();
        }

        private void StopEffect()
        {
            if (_activeEffect) UnityEngine.Object.Destroy(_activeEffect);
            _activeEffect = null;
        }

        public bool CanExecute() => true;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            // 吸引コーンエフェクト（ループ）: 未生成なら起動、生成済みなら位置・向きを追従
            if (_activeEffect == null)
            {
                _activeEffect = ParticlePlayer.PlayLoopAt(_data.impactEffectPrefab, _origin.position);
            }
            else
            {
                _activeEffect.transform.position = _origin.position;
                _activeEffect.transform.rotation = Quaternion.LookRotation(direction);
            }

            int garbageCount = Physics.OverlapSphereNonAlloc(_origin.position, Range, _garbageBuffer, GarbageLayer);
            for (int i = 0; i < garbageCount; i++)
            {
                var toTarget = (_garbageBuffer[i].transform.position - _origin.position).normalized;
                if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;

                if (_garbageBuffer[i].TryGetComponent<GarbageBase>(out var garbage))
                {
                    garbage.Remove();
                }
            }

            // 同じ前方コーン内にいる住人に怒り状態をトリガーする
            int residentCount = Physics.OverlapSphereNonAlloc(_origin.position, Range, _residentBuffer, ResidentLayer);
            for (int i = 0; i < residentCount; i++)
            {
                var toTarget = (_residentBuffer[i].transform.position - _origin.position).normalized;
                if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;
                if (_residentBuffer[i].TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.TriggerAngry();
            }

            if (_data.fireSound != null)
            {
                _audioSource.PlayOneShot(_data.fireSound);
            }
        }
    }
}
