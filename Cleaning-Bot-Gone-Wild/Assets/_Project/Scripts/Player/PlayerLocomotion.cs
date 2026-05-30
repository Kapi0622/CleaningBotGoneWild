using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleaningBot.Player
{
    /// <summary>
    /// プレイヤーの移動・ジャンプ・向きを制御する。
    /// FacingDirectionをWeaponControllerに公開するのが主な責務。
    /// ビジネスロジックは持たない。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Input Actions")]
        [SerializeField] private InputAction _moveAction = new InputAction(
            "Move",
            InputActionType.Value,
            expectedControlType: "Vector2"
        );
        [SerializeField] private InputAction _jumpAction = new InputAction(
            "Jump",
            InputActionType.Button
        );

        private Rigidbody _rb;
        private readonly HashSet<Collider> _groundContacts = new HashSet<Collider>();
        private bool _isGrounded;
        private Vector2 _moveInput;

        /// <summary>
        /// WeaponControllerがExecute()に渡す方向ベクトル。
        /// 移動入力がないときは最後に向いた方向を保持する。
        /// </summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.forward;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;

            // デフォルトバインディングを追加（Inspector未設定時のフォールバック）
            if (_moveAction.bindings.Count == 0)
            {
                _moveAction.AddCompositeBinding("2DVector")
                    .With("Up",    "<Keyboard>/w")
                    .With("Down",  "<Keyboard>/s")
                    .With("Left",  "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                _moveAction.AddCompositeBinding("2DVector")
                    .With("Up",    "<Keyboard>/upArrow")
                    .With("Down",  "<Keyboard>/downArrow")
                    .With("Left",  "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                _moveAction.AddBinding("<Gamepad>/leftStick");

                _jumpAction.AddBinding("<Keyboard>/space");
                _jumpAction.AddBinding("<Gamepad>/buttonSouth");
            }
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();
            _jumpAction.performed += OnJump;
        }

        private void OnDisable()
        {
            _jumpAction.performed -= OnJump;
            _moveAction.Disable();
            _jumpAction.Disable();
        }

        private void Update()
        {
            _moveInput = _moveAction.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_isGrounded)
            {
                _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            }
        }

        private void Move()
        {
            var input = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (input.magnitude > 0.1f)
            {
                var direction = input.normalized;
                FacingDirection = direction;
                transform.forward = direction;

                _rb.MovePosition(_rb.position + direction * _moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // 入力がないときは慣性を殺して止める
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            }
        }

        /// <summary>
        /// PlayerRespawner から呼ばれる。速度をゼロにしてからリスポーン位置へ瞬間移動する。
        /// </summary>
        public void Teleport(Vector3 position)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.position = position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
            {
                _groundContacts.Add(collision.collider);
                _isGrounded = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & _groundLayer) != 0)
            {
                _groundContacts.Remove(collision.collider);
                _isGrounded = _groundContacts.Count > 0;
            }
        }
    }
}
