using System;
using System.Collections.Generic;
using System.Threading;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Player.Weapons;
using CleaningBot.Score;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;


namespace CleaningBot.Player
{
    /// <summary>
    /// 武器の切り替え・発動を制御する。
    /// Input 受付・CancellationToken 管理が責務。ビジネスロジックは各 Strategy に委譲する。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("未設定時は自身の CinemachineImpulseSource を自動取得")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        [Header("Audio")]
        [Tooltip("未設定時は自身の AudioSource を自動取得")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Input Actions")]
        [SerializeField] private InputAction _useAction = new InputAction(
            "Use", InputActionType.Button);
        [SerializeField] private InputAction _switchVacuum = new InputAction(
            "SwitchVacuum", InputActionType.Button);
        [SerializeField] private InputAction _switchRocket = new InputAction(
            "SwitchRocket", InputActionType.Button);
        [SerializeField] private InputAction _switchBlackHole = new InputAction(
            "SwitchBlackHole", InputActionType.Button);

        private IWeaponStrategy _currentStrategy;
        private WeaponStrategyFactory _factory;
        private PlayerLocomotion _locomotion;
        private WeaponModel _weaponModel;
        private IReadOnlyList<WeaponData> _weaponDataList;
        private IReadOnlyList<FloorGrid> _floorGrids;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource _persistentCts;

        private void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_impulseSource == null)
                _impulseSource = GetComponent<CinemachineImpulseSource>();

            if (_useAction.bindings.Count == 0)
            {
                _useAction.AddBinding("<Mouse>/leftButton");
                _useAction.AddBinding("<Gamepad>/rightTrigger");
            }
            if (_switchVacuum.bindings.Count == 0)
            {
                _switchVacuum.AddBinding("<Keyboard>/1");
                _switchVacuum.AddBinding("<Gamepad>/leftShoulder");
            }
            if (_switchRocket.bindings.Count == 0)
            {
                _switchRocket.AddBinding("<Keyboard>/2");
                _switchRocket.AddBinding("<Gamepad>/rightShoulder");
            }
            if (_switchBlackHole.bindings.Count == 0)
            {
                _switchBlackHole.AddBinding("<Keyboard>/3");
                _switchBlackHole.AddBinding("<Gamepad>/buttonEast");
            }
        }

        private void OnEnable()
        {
            _useAction.Enable();
            _switchVacuum.Enable();
            _switchRocket.Enable();
            _switchBlackHole.Enable();
        }

        private void OnDisable()
        {
            _useAction.Disable();
            _switchVacuum.Disable();
            _switchRocket.Disable();
            _switchBlackHole.Disable();
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource(); // 再有効化に備えて新しい CTS を用意
        }

        /// <summary>
        /// Startup.cs から呼ばれる。Factory 生成は WeaponController 内で完結し、
        /// Startup が AudioSource を知る必要をなくす。
        /// </summary>
        public void Initialize(
            WeaponModel weaponModel,
            PlayerLocomotion locomotion,
            IReadOnlyList<FloorGrid> floorGrids,
            IReadOnlyList<WeaponData> weaponDataList)
        {
            if (floorGrids == null) throw new ArgumentNullException(nameof(floorGrids));
            if (floorGrids.Count == 0)
                throw new ArgumentException("floorGrids は空にできません。", nameof(floorGrids));

            // Awake() の実行順序に依存しないよう、ここでも null チェックする
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_impulseSource == null)
                _impulseSource = GetComponent<CinemachineImpulseSource>();

            _weaponModel    = weaponModel;
            _locomotion     = locomotion;
            _weaponDataList = weaponDataList;
            _floorGrids     = floorGrids;
            _persistentCts  = new CancellationTokenSource();
            _factory = new WeaponStrategyFactory(
                weaponDataList, floorGrids, transform, _audioSource,
                () => _locomotion.FacingDirection, _impulseSource, _persistentCts.Token);
            SwitchWeapon(WeaponType.Vacuum);
        }

        /// <summary>
        /// リトライ時に呼ぶ。飛行中のロケット・発動中のブラックホールをキャンセルし、
        /// 永続トークンと Factory を再生成して武器を初期状態（Vacuum）に戻す。
        /// </summary>
        public void ResetEffects()
        {
            _persistentCts.Cancel();
            _persistentCts.Dispose();
            _persistentCts = new CancellationTokenSource();
            _factory = new WeaponStrategyFactory(
                _weaponDataList, _floorGrids, transform, _audioSource,
                () => _locomotion.FacingDirection, _impulseSource, _persistentCts.Token);
            SwitchWeapon(WeaponType.Vacuum);
        }

        public void SwitchWeapon(WeaponType type)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            _currentStrategy?.OnUnequip();
            _currentStrategy = _factory.Create(type);
            _currentStrategy.OnEquip();
            if (_weaponModel != null) _weaponModel.CurrentWeapon.Value = type;
            Debug.Log($"[WeaponController] Switched to: {type}");
        }

        private void Update()
        {
            if (_switchVacuum.WasPressedThisFrame())    SwitchWeapon(WeaponType.Vacuum);
            if (_switchRocket.WasPressedThisFrame())    SwitchWeapon(WeaponType.Rocket);
            if (_switchBlackHole.WasPressedThisFrame()) SwitchWeapon(WeaponType.BlackHole);

            if (_useAction.IsPressed() && _currentStrategy != null && _currentStrategy.CanExecute())
            {
                _currentStrategy.Execute(_locomotion.FacingDirection, _cts.Token);
            }

            if (_useAction.WasReleasedThisFrame() && _currentStrategy != null)
            {
                _currentStrategy.OnExecuteEnd();
            }
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
            _persistentCts?.Cancel();
            _persistentCts?.Dispose();
            _useAction.Dispose();
            _switchVacuum.Dispose();
            _switchRocket.Dispose();
            _switchBlackHole.Dispose();
        }
    }
}
