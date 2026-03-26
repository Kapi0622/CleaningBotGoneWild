using System;
using System.Threading;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using Cysharp.Threading.Tasks;
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

        private readonly WeaponData    _data;
        private readonly FloorGrid     _floorGrid;
        private readonly Transform     _origin;
        private readonly AudioSource   _audioSource;
        private readonly Func<Vector3> _getFacingDirection;

        private float      _lastFireTime = float.MinValue;
        private GameObject _indicator;

        public BlackHoleStrategy(
            WeaponData data,
            FloorGrid floorGrid,
            Transform origin,
            AudioSource audioSource,
            Func<Vector3> getFacingDirection)
        {
            _data               = data;
            _floorGrid          = floorGrid;
            _origin             = origin;
            _audioSource        = audioSource;
            _getFacingDirection = getFacingDirection;
        }

        public void OnEquip()
        {
            _indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _indicator.name = "BlackHoleLandingIndicator";
            _indicator.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
            UnityEngine.Object.Destroy(_indicator.GetComponent<CapsuleCollider>());

            var rend = _indicator.GetComponent<Renderer>();
            var mat  = rend.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f));
            else
                mat.color = new Color(1f, 0.4f, 0f);

            var comp = _indicator.AddComponent<BlackHoleLandingIndicator>();
            comp.Initialize(ComputeCurrentLandingPoint);
        }

        public void OnUnequip()
        {
            if (_indicator) UnityEngine.Object.Destroy(_indicator);
            _indicator = null;
        }

        public bool CanExecute() => Time.time >= _lastFireTime + _data.cooldown;

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            _lastFireTime = Time.time;
            BlackHoleAsync(direction, ct).Forget();
        }

        /// <summary>現在の向きと位置から着地予測点を返す。BlackHoleLandingIndicator に渡すデリゲート。</summary>
        private Vector3 ComputeCurrentLandingPoint()
        {
            if (!_origin) return Vector3.zero;
            float flightTime      = ComputeFlightTime(_origin.position.y);
            float horizontalSpeed = SafeHorizontalSpeed(flightTime);
            var   dir             = _getFacingDirection();
            var   pos             = GetArcPosition(_origin.position, dir, flightTime, horizontalSpeed);
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
            var   arcPoints       = ComputeArcPoints(startPos, direction, flightTime, horizontalSpeed);

            var landingPoint = arcPoints[arcPoints.Length - 1];
            landingPoint.y   = FloorY;

            // ---- 軌道ライン ----
            var lineObj = new GameObject("BlackHoleTrajectory");
            var lr      = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = arcPoints.Length;
            lr.SetPositions(arcPoints);
            lr.startWidth = 0.1f;
            lr.endWidth   = 0.05f;
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit != null)
            {
                var mat = new Material(urpUnlit);
                mat.SetColor("_BaseColor", Color.cyan);
                lr.material = mat;
            }

            // ---- 投擲物 Sphere ----
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.localScale = Vector3.one * 0.3f;
            projectile.transform.position   = startPos;
            UnityEngine.Object.Destroy(projectile.GetComponent<SphereCollider>());
            var projRenderer = projectile.GetComponent<Renderer>();
            projRenderer.material.color = Color.black;

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

                // ---- Phase 2: 着弾 — ライン非表示、ブラックホール拡大 ----
                lr.enabled = false;
                projectile.transform.position   = landingPoint;
                projectile.transform.localScale = Vector3.one * 1.5f;

                int   tickCount     = Mathf.RoundToInt(SuctionDuration / DamageTickInterval);
                int   perTickDamage = Mathf.Max(1, Mathf.RoundToInt(_data.floorDamage / tickCount));
                float floorRadius   = _data.blastRadius * FloorRadiusFactor;
                float removeRadius  = _data.blastRadius * GarbageRemoveRadiusFactor;

                for (int i = 0; i < tickCount; i++)
                {
                    await UniTask.Delay(
                        (int)(DamageTickInterval * 1000),
                        cancellationToken: ct);

                    _floorGrid.ApplyDamage(landingPoint, floorRadius, perTickDamage);
                    AttractAndRemoveGarbage(landingPoint, removeRadius);
                }

                // ---- Phase 3: 最終クリーンアップ ----
                FinalCleanup(landingPoint);
            }
            finally
            {
                if (lineObj)    UnityEngine.Object.Destroy(lineObj);
                if (projectile) UnityEngine.Object.Destroy(projectile);
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
            float g            = Physics.gravity.y; // 負の値
            float a            = 0.5f * g;
            float b            = UpwardSpeed;
            float c            = startY - FloorY;
            float discriminant = b * b - 4f * a * c;
            // g < 0 なので (-b - sqrt(D)) / (2a) が正の解
            return (-b - Mathf.Sqrt(Mathf.Max(0f, discriminant))) / (2f * a);
        }

        private static Vector3[] ComputeArcPoints(
            Vector3 startPos, Vector3 direction, float flightTime, float horizontalSpeed)
        {
            var points = new Vector3[ArcResolution];
            for (int i = 0; i < ArcResolution; i++)
            {
                float t   = flightTime * i / (ArcResolution - 1);
                points[i] = GetArcPosition(startPos, direction, t, horizontalSpeed);
            }
            return points;
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
            var hits = Physics.OverlapSphere(center, _data.blastRadius, GarbageLayer);
            foreach (var col in hits)
            {
                if (!col.TryGetComponent<GarbageBase>(out var garbage)) continue;

                float dist = Vector3.Distance(col.transform.position, center);
                if (dist < removeRadius)
                {
                    garbage.Remove();
                }
                else
                {
                    var inDir = (center - col.transform.position).normalized;
                    if (col.TryGetComponent<Rigidbody>(out var rb))
                        rb.AddForce(inDir * GarbageAttractForce, ForceMode.Impulse);
                    else
                        col.transform.position = Vector3.MoveTowards(
                            col.transform.position, center, GarbageAttractStep);
                }
            }
        }

        /// <summary>吸引終了時に残っているゴミを除去し、住人を内向きに吹き飛ばす。</summary>
        private void FinalCleanup(Vector3 center)
        {
            var garbageHits = Physics.OverlapSphere(center, _data.blastRadius, GarbageLayer);
            foreach (var col in garbageHits)
                if (col.TryGetComponent<GarbageBase>(out var garbage))
                    garbage.Remove();

            var residentHits = Physics.OverlapSphere(center, _data.blastRadius, ResidentLayer);
            foreach (var r in residentHits)
            {
                var inDir = (center - r.transform.position).normalized;
                if (inDir == Vector3.zero) inDir = Vector3.up;
                if (r.TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.OnHit(inDir, _data.residentHitForce);
            }
        }
    }
}
