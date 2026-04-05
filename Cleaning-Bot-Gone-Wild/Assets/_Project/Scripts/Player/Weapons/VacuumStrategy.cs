using System.Threading;
using CleaningBot.Data;
using CleaningBot.Effects;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using Unity.Cinemachine;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// 掃除機ストラテジー。
    /// クールタイムなし。向いた方向の短射程内にあるゴミを即時吸引・除去する。
    /// </summary>
    public class VacuumStrategy : IWeaponStrategy
    {
        // 前方 90° の半球コーン（cos90° = 0）。背後のゴミは除去しない
        private const float HalfConeCos = 0f;
        private static readonly int GarbageLayer  = LayerMask.GetMask("Garbage");
        private static readonly int ResidentLayer = LayerMask.GetMask("Resident");

        // OverlapSphereNonAlloc 用バッファ
        private readonly Collider[] _garbageBuffer  = new Collider[32];
        private readonly Collider[] _residentBuffer = new Collider[16];

        private readonly WeaponData              _data;
        private readonly Transform               _origin;
        private readonly AudioSource             _audioSource;
        private readonly CinemachineImpulseSource _impulseSource;

        private GameObject _activeEffect;
        private GameObject _indicator;
        private bool _fireSoundActive;
        private float _lastDamageTime = float.MinValue;

        public VacuumStrategy(WeaponData data, Transform origin, AudioSource audioSource, CinemachineImpulseSource impulseSource)
        {
            _data          = data;
            _origin        = origin;
            _audioSource   = audioSource;
            _impulseSource = impulseSource;
        }

        public void OnEquip()
        {
            float radius = Mathf.Max(_data.blastRadius, 0.1f);
            if (_data.rangeIndicatorPrefab != null)
            {
                _indicator = UnityEngine.Object.Instantiate(_data.rangeIndicatorPrefab);
                // Prefab は単位スケール (radius=1) で作成されているため blastRadius に合わせてスケール
                var s = _indicator.transform.localScale;
                _indicator.transform.localScale = new Vector3(radius * s.x, s.y, radius * s.z);
            }
            else
            {
                _indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _indicator.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
                UnityEngine.Object.Destroy(_indicator.GetComponent<CapsuleCollider>());
                var rend = _indicator.GetComponent<Renderer>();
                var mpb  = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(0.3f, 0.8f, 1f, 0.3f));
                rend.SetPropertyBlock(mpb);
            }
            _indicator.name = "VacuumRangeIndicator";
            _indicator.transform.SetParent(_origin);
            _indicator.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            _indicator.transform.localRotation = Quaternion.identity;
        }

        public void OnUnequip()
        {
            if (_indicator) UnityEngine.Object.Destroy(_indicator);
            _indicator = null;
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
            if (_fireSoundActive)
            {
                _audioSource.loop = false;
                _audioSource.Stop();
                _fireSoundActive = false;
            }
        }

        public bool CanExecute() => true;

        /// <summary>
        /// NonAlloc でオーバーラップを取得する。バッファが埋まった場合（超過の可能性あり）は
        /// Alloc 版にフォールバックして取りこぼしを防ぐ。
        /// 通常ケースはゼロアロック。
        /// </summary>
        private static Collider[] OverlapSphereWithFallback(
            Vector3 pos, float radius, Collider[] buffer, int layerMask, out int count)
        {
            count = Physics.OverlapSphereNonAlloc(pos, radius, buffer, layerMask);
            if (count < buffer.Length) return buffer;
            // バッファが埋まった → 超過分が切り捨てられている可能性があるため Alloc 版で再取得
            var allHits = Physics.OverlapSphere(pos, radius, layerMask);
            count = allHits.Length;
            return allHits;
        }

        public void Execute(Vector3 direction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            // 吸引コーンエフェクト（ループ）: 未生成なら起動、生成済みなら位置・向きを追従
            var effectRotation = direction != Vector3.zero
                ? Quaternion.LookRotation(direction)
                : _origin.rotation;

            if (_activeEffect == null)
            {
                _activeEffect = ParticlePlayer.PlayLoopAt(_data.impactEffectPrefab, _origin.position, effectRotation);
                if (_data.shakeIntensity > 0f)
                    _impulseSource?.GenerateImpulse(_data.shakeIntensity);
            }
            else
            {
                _activeEffect.transform.SetPositionAndRotation(_origin.position, effectRotation);
            }

            var garbageHits = OverlapSphereWithFallback(_origin.position, _data.blastRadius, _garbageBuffer, GarbageLayer, out int garbageCount);
            bool canDamage = _data.damageInterval <= 0f
                || Time.time >= _lastDamageTime + _data.damageInterval;

            if (canDamage)
            {
                bool didDamage = false;
                for (int i = 0; i < garbageCount; i++)
                {
                    var toTarget = (garbageHits[i].transform.position - _origin.position).normalized;
                    if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;

                    if (garbageHits[i].TryGetComponent<GarbageBase>(out var garbage))
                    {
                        garbage.TakeDamage(_data.garbageDamage);
                        didDamage = true;
                    }
                }
                if (didDamage) _lastDamageTime = Time.time;
            }

            // 同じ前方コーン内にいる住人に怒り状態をトリガーする
            var residentHits = OverlapSphereWithFallback(_origin.position, _data.blastRadius, _residentBuffer, ResidentLayer, out int residentCount);
            for (int i = 0; i < residentCount; i++)
            {
                var toTarget = (residentHits[i].transform.position - _origin.position).normalized;
                if (Vector3.Dot(direction, toTarget) < HalfConeCos) continue;
                if (residentHits[i].TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.TriggerAngry();
            }

            if (!_fireSoundActive && _data.fireSound != null)
            {
                _audioSource.clip = _data.fireSound;
                _audioSource.loop = true;
                _audioSource.Play();
                _fireSoundActive = true;
            }
        }
    }
}
