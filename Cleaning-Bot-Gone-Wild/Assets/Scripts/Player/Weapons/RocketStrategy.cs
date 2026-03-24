using System.Threading;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Garbage;
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
        private static readonly int GarbageLayer = LayerMask.GetMask("Garbage");
        private static readonly int HitLayer = LayerMask.GetMask("Garbage", "Environment");

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
            var hitPoint = Physics.Raycast(ray, out var hit, RayDistance, HitLayer)
                ? hit.point
                : _origin.position + direction * RayDistance;

            // 着弾点の爆発範囲内にあるゴミを OverlapSphere で一括検出・除去
            // Raycast が壁に当たっても、壁際のゴミは爆風で消える
            var explosionRadius = Mathf.Max(_data.blastRadius, 0.5f);
            var garbageHits = Physics.OverlapSphere(hitPoint, explosionRadius, GarbageLayer);
            foreach (var g in garbageHits)
            {
                if (g.TryGetComponent<GarbageBase>(out var garbage))
                {
                    garbage.Remove();
                }
            }

            // STEP 5 で本実装。現時点は NoOp（メソッドは存在する）
            _floorGrid.ApplyDamage(hitPoint, explosionRadius, (int)_data.floorDamage);

            await UniTask.Delay(200, cancellationToken: ct);
        }
    }
}
