using System.Threading;
using CleaningBot.Data;
using CleaningBot.Effects;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Unity.Cinemachine;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// ロケットストラテジー。
    /// クールタイムあり。向いた方向にRaycastを飛ばし、着弾点のゴミを除去・床にダメージを与える。
    /// 発射後は可視弾（Sphere）が飛行し、着弾後に爆発エフェクトを再生する。
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
        private readonly CinemachineImpulseSource _impulseSource;
        private readonly CancellationToken _persistentCt;

        private float _lastFireTime = float.MinValue;

        public RocketStrategy(WeaponData data, FloorGrid floorGrid, Transform origin, AudioSource audioSource, CinemachineImpulseSource impulseSource, CancellationToken persistentCt)
        {
            _data          = data;
            _floorGrid     = floorGrid;
            _origin        = origin;
            _audioSource   = audioSource;
            _impulseSource = impulseSource;
            _persistentCt  = persistentCt;
        }

        public void OnEquip() { }
        public void OnUnequip() { }
        public void OnExecuteEnd() { }

        public bool CanExecute() => Time.time >= _lastFireTime + _data.cooldown;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            _lastFireTime = Time.time;
            // 意図的に _persistentCt を使用: 武器切り替え後も弾が飛び続け着弾・爆発する仕様
            FireAsync(direction, _persistentCt).Forget();
        }

        private async UniTaskVoid FireAsync(Vector3 direction, CancellationToken ct)
        {
            if (_data.fireSound != null)
                _audioSource.PlayOneShot(_data.fireSound);

            // Raycast で着弾点を決定
            var ray = new Ray(_origin.position, direction);
            bool didHit = Physics.Raycast(ray, out var hit, RayDistance, HitLayer);
            var startPos = _origin.position;
            var hitPoint = didHit ? hit.point : startPos + direction * RayDistance;

            // 可視弾（Sphere）の生成
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "RocketProjectile";
            projectile.transform.localScale = Vector3.one * 0.3f;
            projectile.transform.position   = startPos;
            UnityEngine.Object.Destroy(projectile.GetComponent<SphereCollider>());
            var projRenderer = projectile.GetComponent<Renderer>();
            var projMpb      = new MaterialPropertyBlock();
            projRenderer.GetPropertyBlock(projMpb);
            projMpb.SetColor("_BaseColor", new Color(1f, 0.4f, 0.1f));
            projRenderer.SetPropertyBlock(projMpb);

            // 飛行時間 = 距離 / 弾速
            float distance = Vector3.Distance(startPos, hitPoint);
            float duration = distance / Mathf.Max(_data.projectileSpeed, 0.1f);

            var handle = LMotion.Create(startPos, hitPoint, duration)
                .WithEase(Ease.Linear)
                .BindToPosition(projectile.transform);

            try
            {
                await handle.ToUniTask(ct);

                // 着弾後: 爆発エフェクト・SE・カメラシェイク
                var explosionRadius = Mathf.Max(_data.blastRadius, 0.5f);
                ParticlePlayer.PlayAt(_data.impactEffectPrefab, hitPoint);
                if (_data.impactSound != null)
                    _audioSource.PlayOneShot(_data.impactSound);
                _impulseSource?.GenerateImpulse(_data.shakeIntensity);

                // 爆発範囲内のゴミを除去
                var garbageHits = OverlapSphereWithFallback(hitPoint, explosionRadius, _garbageBuffer, GarbageLayer, out int garbageCount);
                for (int i = 0; i < garbageCount; i++)
                {
                    if (garbageHits[i].TryGetComponent<GarbageBase>(out var garbage))
                        garbage.Remove();
                }

                _floorGrid.ApplyDamage(hitPoint, explosionRadius, (int)_data.floorDamage);

                // 爆発範囲内の住人を吹き飛ばす
                var residentHits = OverlapSphereWithFallback(hitPoint, explosionRadius, _residentBuffer, ResidentLayer, out int residentCount);
                for (int i = 0; i < residentCount; i++)
                {
                    var outDir = (residentHits[i].transform.position - hitPoint).normalized;
                    if (outDir == Vector3.zero) outDir = Vector3.up;
                    if (residentHits[i].TryGetComponent<ResidentReactor>(out var reactor))
                        reactor.OnHit(outDir, _data.residentHitForce);
                }
            }
            finally
            {
                if (handle.IsActive()) handle.Cancel();
                if (projectile) UnityEngine.Object.Destroy(projectile);
            }
        }

        private static Collider[] OverlapSphereWithFallback(
            Vector3 pos, float radius, Collider[] buffer, int layerMask, out int count)
        {
            count = Physics.OverlapSphereNonAlloc(pos, radius, buffer, layerMask);
            if (count < buffer.Length) return buffer;
            var allHits = Physics.OverlapSphere(pos, radius, layerMask);
            count = allHits.Length;
            return allHits;
        }
    }
}
