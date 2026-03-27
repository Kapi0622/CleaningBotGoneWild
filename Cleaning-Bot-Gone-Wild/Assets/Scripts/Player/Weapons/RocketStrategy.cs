using System.Threading;
using CleaningBot.Data;
using CleaningBot.Effects;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// ロケットストラテジー。
    /// クールタイムあり。向いた方向にRaycastを飛ばし、着弾点のゴミを除去・床にダメージを与える。
    /// UniTask + CancellationToken で非同期演出を管理する。
    /// </summary>
    public class RocketStrategy : IWeaponStrategy
    {
        private const float RayDistance = 20f;
        private static readonly int GarbageLayer  = LayerMask.GetMask("Garbage");
        private static readonly int HitLayer      = LayerMask.GetMask("Garbage", "Environment");
        private static readonly int ResidentLayer = LayerMask.GetMask("Resident");

        // OverlapSphereNonAlloc 用バッファ
        private readonly Collider[] _garbageBuffer  = new Collider[32];
        private readonly Collider[] _residentBuffer = new Collider[16];

        private readonly WeaponData _data;
        private readonly FloorGrid _floorGrid;
        private readonly Transform _origin;
        private readonly AudioSource _audioSource;

        private float _lastFireTime = float.MinValue;

        public RocketStrategy(WeaponData data, FloorGrid floorGrid, Transform origin, AudioSource audioSource)
        {
            _data = data;
            _floorGrid = floorGrid;
            _origin = origin;
            _audioSource = audioSource;
        }

        public void OnEquip() { }
        public void OnUnequip() { }
        public void OnExecuteEnd() { }

        public bool CanExecute() => Time.time >= _lastFireTime + _data.cooldown;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            _lastFireTime = Time.time;
            FireAsync(direction, ct).Forget();
        }

        private async UniTaskVoid FireAsync(Vector3 direction, CancellationToken ct)
        {
            if (_data.fireSound != null)
            {
                _audioSource.PlayOneShot(_data.fireSound);
            }

            // Raycast で着弾点を決定。Environment（壁）にヒットしても着弾点は確定する
            var ray = new Ray(_origin.position, direction);
            bool didHit = Physics.Raycast(ray, out var hit, RayDistance, HitLayer);
            var hitPoint = didHit ? hit.point : _origin.position + direction * RayDistance;

            ParticlePlayer.PlayAt(_data.impactEffectPrefab, hitPoint);

            // 着弾点の爆発範囲内にあるゴミを NonAlloc で一括検出・除去
            var explosionRadius = Mathf.Max(_data.blastRadius, 0.5f);
            int garbageCount = Physics.OverlapSphereNonAlloc(hitPoint, explosionRadius, _garbageBuffer, GarbageLayer);
            for (int i = 0; i < garbageCount; i++)
            {
                if (_garbageBuffer[i].TryGetComponent<GarbageBase>(out var garbage))
                {
                    garbage.Remove();
                }
            }

            _floorGrid.ApplyDamage(hitPoint, explosionRadius, (int)_data.floorDamage);

            // 爆発範囲内の住人を吹き飛ばす
            int residentCount = Physics.OverlapSphereNonAlloc(hitPoint, explosionRadius, _residentBuffer, ResidentLayer);
            for (int i = 0; i < residentCount; i++)
            {
                var outDir = (_residentBuffer[i].transform.position - hitPoint).normalized;
                if (outDir == Vector3.zero) outDir = Vector3.up;
                if (_residentBuffer[i].TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.OnHit(outDir, _data.residentHitForce);
            }

            await UniTask.Delay(200, cancellationToken: ct);
        }
    }
}
