using System;
using System.Threading;
using CleaningBot.Data;
using CleaningBot.Effects;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// ブラックホールストラテジー。
    /// 向いた方向へ放物線投擲。軌道を LineRenderer で可視化。装備中は着地予測マーカーを表示。
    /// 着弾後は一定時間持続してゴミを吸引・床にダメージを蓄積し、最後に住人を引き寄せる。
    /// </summary>
    public class BlackHoleStrategy : IWeaponStrategy
    {
        private const float UpwardSpeed               = 7f;
        private const int   ArcResolution             = 30;
        private const float FloorY                    = 0.1f;
        private const float SuctionDuration           = 2.0f;
        private const float DamageTickInterval        = 0.5f;
        private const float FloorRadiusFactor         = 0.6f;
        private const float GarbageRemoveRadiusFactor = 0.25f;
        private const float GarbageAttractForce       = 6f;
        private const float GarbageAttractStep        = 1.0f;

        private static readonly int GarbageLayer  = LayerMask.GetMask("Garbage");
        private static readonly int ResidentLayer = LayerMask.GetMask("Resident");

        private readonly WeaponData                _data;
        private readonly FloorGrid                 _floorGrid;
        private readonly Transform                 _origin;
        private readonly AudioSource               _audioSource;
        private readonly Func<Vector3>             _getFacingDirection;
        private readonly CinemachineImpulseSource  _impulseSource;
        private readonly CancellationToken         _persistentCt;

        // キャッシュ: 毎回 new Vector3[] を回避
        private readonly Vector3[] _arcPoints = new Vector3[ArcResolution];


        // OverlapSphereNonAlloc 用バッファ
        private readonly Collider[] _garbageBuffer  = new Collider[32];
        private readonly Collider[] _residentBuffer = new Collider[16];

        private float      _lastFireTime = float.MinValue;
        private GameObject _indicator;
        private GameObject _activeVortex;

        public BlackHoleStrategy(
            WeaponData data,
            FloorGrid floorGrid,
            Transform origin,
            AudioSource audioSource,
            Func<Vector3> getFacingDirection,
            CinemachineImpulseSource impulseSource,
            CancellationToken persistentCt)
        {
            _data               = data;
            _floorGrid          = floorGrid;
            _origin             = origin;
            _audioSource        = audioSource;
            _getFacingDirection = getFacingDirection ?? throw new ArgumentNullException(nameof(getFacingDirection));
            _impulseSource      = impulseSource;
            _persistentCt       = persistentCt;
        }

        public void OnEquip()
        {
            if (_data.indicatorPrefab != null)
            {
                _indicator = UnityEngine.Object.Instantiate(_data.indicatorPrefab);
            }
            else
            {
                // indicatorPrefab 未設定時のフォールバック
                _indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _indicator.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
                UnityEngine.Object.Destroy(_indicator.GetComponent<CapsuleCollider>());

                var rend = _indicator.GetComponent<Renderer>();
                var mpb  = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(1f, 0.4f, 0f));
                rend.SetPropertyBlock(mpb);
            }

            _indicator.name = "BlackHoleLandingIndicator";
            var comp = _indicator.GetComponent<BlackHoleLandingIndicator>()
                       ?? _indicator.AddComponent<BlackHoleLandingIndicator>();
            comp.Initialize(ComputeCurrentLandingPoint);
        }

        public void OnUnequip()
        {
            if (_indicator) UnityEngine.Object.Destroy(_indicator);
            _indicator = null;
        }

        public void OnExecuteEnd() { }

        public bool CanExecute() => Time.time >= _lastFireTime + _data.cooldown;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            _lastFireTime = Time.time;
            // 意図的に _persistentCt を使用: 武器切り替え後もブラックホールが継続して吸引・ダメージを与える仕様
            BlackHoleAsync(direction, _persistentCt).Forget();
        }

        /// <summary>現在の向きと位置から着地予測点を返す。BlackHoleLandingIndicator に渡すデリゲート。</summary>
        private Vector3 ComputeCurrentLandingPoint()
        {
            if (!_origin) return Vector3.zero;
            float flightTime      = ComputeFlightTime(_origin.position.y);
            float horizontalSpeed = SafeHorizontalSpeed(flightTime);
            var   dir             = _getFacingDirection();
            ComputeArcPoints(_origin.position, dir, flightTime, horizontalSpeed);
            var pos = _arcPoints[ArcResolution - 1];
            pos.y = FloorY;
            return pos;
        }

        private async UniTaskVoid BlackHoleAsync(Vector3 direction, CancellationToken ct)
        {
            if (_data.fireSound != null)
                _audioSource.PlayOneShot(_data.fireSound);

            var   startPos        = _origin.position;
            float flightTime      = ComputeFlightTime(startPos.y);
            float horizontalSpeed = SafeHorizontalSpeed(flightTime);
            ComputeArcPoints(startPos, direction, flightTime, horizontalSpeed);

            var landingPoint = _arcPoints[ArcResolution - 1];
            landingPoint.y   = FloorY;

            // ---- 軌道ライン ----
            var lineObj = new GameObject("BlackHoleTrajectory");
            var lr      = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = ArcResolution;
            lr.SetPositions(_arcPoints);
            lr.startWidth = 0.1f;
            lr.endWidth   = 0.05f;

            if (_data.trajectoryLineMaterial != null) lr.material = _data.trajectoryLineMaterial;

            // ---- 投擲物 Sphere ----
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.localScale = Vector3.one * 0.3f;
            projectile.transform.position   = startPos;
            UnityEngine.Object.Destroy(projectile.GetComponent<SphereCollider>());
            var projRenderer = projectile.GetComponent<Renderer>();
            var projMpb      = new MaterialPropertyBlock();
            projRenderer.GetPropertyBlock(projMpb);
            projMpb.SetColor("_BaseColor", Color.black);
            projRenderer.SetPropertyBlock(projMpb);

            try
            {
                // ---- Phase 1: 放物線飛行 ----
                float elapsed = 0f;
                while (elapsed < flightTime)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
                    elapsed += Time.deltaTime;
                    projectile.transform.position =
                        GetArcPosition(startPos, direction, Mathf.Min(elapsed, flightTime), horizontalSpeed);
                }

                // ---- Phase 2: 着弾 — ライン非表示、ブラックホール拡大、ボルテックスエフェクト ----
                lr.enabled = false;
                if (_indicator) { UnityEngine.Object.Destroy(_indicator); _indicator = null; }
                projectile.transform.position   = landingPoint;
                projectile.transform.localScale = Vector3.one * 1.5f;
                _activeVortex = ParticlePlayer.PlayLoopAt(_data.impactEffectPrefab, landingPoint);
                if (_data.impactSound != null)
                    _audioSource.PlayOneShot(_data.impactSound);
                _impulseSource?.GenerateImpulse(_data.shakeIntensity);

                int   tickCount   = Mathf.Max(1, Mathf.CeilToInt(SuctionDuration / DamageTickInterval));
                int   totalDamage = Mathf.RoundToInt(_data.floorDamage);
                int   baseD       = totalDamage / tickCount;
                int   rem         = totalDamage % tickCount;
                float floorRadius  = _data.blastRadius * FloorRadiusFactor;
                float removeRadius = _data.blastRadius * GarbageRemoveRadiusFactor;

                for (int i = 0; i < tickCount; i++)
                {
                    await UniTask.Delay(
                        (int)(DamageTickInterval * 1000),
                        cancellationToken: ct);

                    int thisTick = baseD + (i < rem ? 1 : 0);
                    _floorGrid.ApplyDamage(landingPoint, floorRadius, thisTick);
                    AttractAndRemoveGarbage(landingPoint, removeRadius);
                }

                // ---- Phase 3: 最終クリーンアップ ----
                FinalCleanup(landingPoint);
            }
            finally
            {
                if (lineObj)       UnityEngine.Object.Destroy(lineObj);
                if (projectile)    UnityEngine.Object.Destroy(projectile);
                if (_activeVortex) UnityEngine.Object.Destroy(_activeVortex);
                _activeVortex = null;
            }
        }

        /// <summary>throwRange が 0 以下のとき最低速度（1m/s）にクランプする。</summary>
        private float SafeHorizontalSpeed(float flightTime)
        {
            float range = Mathf.Max(_data.throwRange, 1f);
            return range / flightTime;
        }

        /// <summary>放物線の着弾時間を二次方程式で求める。</summary>
        private static float ComputeFlightTime(float startY)
        {
            float g            = Physics.gravity.y;
            float a            = 0.5f * g;
            float b            = UpwardSpeed;
            float c            = startY - FloorY;
            float discriminant = b * b - 4f * a * c;
            return (-b - Mathf.Sqrt(Mathf.Max(0f, discriminant))) / (2f * a);
        }

        /// <summary>キャッシュ済み _arcPoints バッファに放物線座標を書き込む。</summary>
        private void ComputeArcPoints(Vector3 startPos, Vector3 direction, float flightTime, float horizontalSpeed)
        {
            for (int i = 0; i < ArcResolution; i++)
            {
                float t    = flightTime * i / (ArcResolution - 1);
                _arcPoints[i] = GetArcPosition(startPos, direction, t, horizontalSpeed);
            }
        }

        private static Vector3 GetArcPosition(
            Vector3 startPos, Vector3 direction, float t, float horizontalSpeed)
        {
            return new Vector3(
                startPos.x + direction.x * horizontalSpeed * t,
                startPos.y + UpwardSpeed  * t + 0.5f * Physics.gravity.y * t * t,
                startPos.z + direction.z  * horizontalSpeed * t);
        }

        /// <summary>ゴミを吸引半径内に引き寄せ、消滅半径内に入ったものを除去する。</summary>
        private void AttractAndRemoveGarbage(Vector3 center, float removeRadius)
        {
            var hits = OverlapSphereWithFallback(center, _data.blastRadius, _garbageBuffer, GarbageLayer, out int count);
            for (int i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent<GarbageBase>(out var garbage)) continue;

                float dist = Vector3.Distance(hits[i].transform.position, center);
                if (dist < removeRadius)
                {
                    garbage.TakeDamage(_data.garbageDamage);
                }
                else
                {
                    var inDir = (center - hits[i].transform.position).normalized;
                    if (hits[i].TryGetComponent<Rigidbody>(out var rb))
                        rb.AddForce(inDir * GarbageAttractForce, ForceMode.Impulse);
                    else
                        hits[i].transform.position = Vector3.MoveTowards(
                            hits[i].transform.position, center, GarbageAttractStep);
                }
            }
        }

        /// <summary>吸引終了時に残っているゴミを除去し、住人を内向きに吹き飛ばす。</summary>
        private void FinalCleanup(Vector3 center)
        {
            var garbageHits = OverlapSphereWithFallback(center, _data.blastRadius, _garbageBuffer, GarbageLayer, out int garbageCount);
            for (int i = 0; i < garbageCount; i++)
                if (garbageHits[i].TryGetComponent<GarbageBase>(out var garbage))
                    garbage.TakeDamage(_data.garbageDamage);

            var residentHits = OverlapSphereWithFallback(center, _data.blastRadius, _residentBuffer, ResidentLayer, out int residentCount);
            for (int i = 0; i < residentCount; i++)
            {
                var inDir = (center - residentHits[i].transform.position).normalized;
                if (inDir == Vector3.zero) inDir = Vector3.up;
                if (residentHits[i].TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.OnHit(inDir, _data.residentHitForce);
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
