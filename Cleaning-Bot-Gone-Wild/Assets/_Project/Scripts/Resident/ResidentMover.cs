using UnityEngine;

namespace CleaningBot.Resident
{
    public enum ResidentState { Idle, Flee, Hit, Angry }

    /// <summary>
    /// 住人の移動 AI。4 状態ステートマシンで NavMesh を使わないシンプルステアリングを実装する。
    /// 状態優先度: Hit > Flee > Angry > Idle
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ResidentMover : MonoBehaviour
    {
        [SerializeField] private float _idleMoveSpeed    = 1.5f;
        [SerializeField] private float _fleeMoveSpeed    = 4f;
        [SerializeField] private float _wanderRadius     = 8f;
        [SerializeField] private float _arrivalThreshold = 0.3f;
        [SerializeField] private float _hitRecoveryTime  = 1.5f;

        private float         _speedMultiplier = 1.0f;
        private Rigidbody     _rb;
        private Transform     _playerTransform;
        private ResidentState _state = ResidentState.Idle;
        private Vector3       _wanderTarget;
        private float         _hitRecoveryTimer;

        public ResidentState CurrentState => _state;

        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// 外部から状態を要求する。優先度ルールに従い受け入れ可否を判断する。
        /// Hit は常に受け入れる / Flee は Hit 中以外 / Angry は Idle 中のみ / Idle は Hit・Flee 中は拒否
        /// </summary>
        public void SetState(ResidentState next)
        {
            bool canTransition = next switch
            {
                ResidentState.Hit   => true,
                ResidentState.Flee  => _state != ResidentState.Hit,
                ResidentState.Angry => _state == ResidentState.Idle,
                ResidentState.Idle  => _state != ResidentState.Hit && _state != ResidentState.Flee,
                _                   => false
            };
            if (!canTransition) return;

            _state = next;
            if (next == ResidentState.Hit)  _hitRecoveryTimer = _hitRecoveryTime;
            if (next == ResidentState.Idle) PickNewWanderTarget();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            PickNewWanderTarget();
        }

        private void FixedUpdate()
        {
            switch (_state)
            {
                case ResidentState.Idle:  UpdateIdle();  break;
                case ResidentState.Flee:  UpdateFlee();  break;
                case ResidentState.Hit:   UpdateHit();   break;
                case ResidentState.Angry: UpdateAngry(); break;
            }
        }

        private void UpdateIdle()
        {
            var next = Vector3.MoveTowards(_rb.position, _wanderTarget,
                _idleMoveSpeed * _speedMultiplier * Time.fixedDeltaTime);
            _rb.MovePosition(next);

            var dir = (_wanderTarget - _rb.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.forward = dir.normalized;

            if (Vector3.Distance(_rb.position, _wanderTarget) < _arrivalThreshold)
                PickNewWanderTarget();
        }

        private void UpdateFlee()
        {
            if (_playerTransform == null) return;

            var dir = (transform.position - _playerTransform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            dir.Normalize();

            _rb.MovePosition(_rb.position + dir * (_fleeMoveSpeed * _speedMultiplier * Time.fixedDeltaTime));
            transform.forward = dir;
        }

        private void UpdateHit()
        {
            // Rigidbody の物理に任せる。MovePosition は呼ばない。
            _hitRecoveryTimer -= Time.fixedDeltaTime;
            if (_hitRecoveryTimer <= 0f)
                SetState(ResidentState.Idle);
        }

        private void UpdateAngry()
        {
            if (_playerTransform == null) return;

            // プレイヤーの反対方向を向く。移動はしない。
            var dir = (transform.position - _playerTransform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.forward = dir.normalized;
        }

        private void PickNewWanderTarget()
        {
            var offset = new Vector3(
                Random.Range(-_wanderRadius, _wanderRadius),
                0f,
                Random.Range(-_wanderRadius, _wanderRadius));
            _wanderTarget = transform.position + offset;
        }
    }
}
