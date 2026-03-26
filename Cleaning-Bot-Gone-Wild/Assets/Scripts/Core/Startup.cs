using System.Collections.Generic;
using System.Linq;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Player;
using CleaningBot.Presenter;
using CleaningBot.Resident;
using CleaningBot.Score;
using CleaningBot.View;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 依存関係の解決と初期化順序の管理のみ。
    /// ロジック・条件分岐・スコア計算は絶対に書かない。
    /// </summary>
    public class Startup : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerLocomotion _playerLocomotion;
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private FloorGrid _floorGrid;
        [SerializeField] private GameTimer _gameTimer;

        [Header("Stage")]
        [SerializeField] private StageData _stageData;
        [SerializeField] private List<WeaponData> _weaponDataList;
        [SerializeField] private List<GarbageBase> _garbages;
        [SerializeField] private List<ResidentMover> _residentMovers;

        [Header("Views")]
        [SerializeField] private ScoreView _scoreView;
        [SerializeField] private TimerView _timerView;
        [SerializeField] private WeaponView _weaponView;
        [SerializeField] private GarbageView _garbageView;

        private void Awake()
        {
            // 1. Model 生成
            var scoreModel   = new ScoreModel();
            var timerModel   = new TimerModel(_stageData.timeLimit);
            var weaponModel  = new WeaponModel();
            var garbageModel = new GarbageModel();

            // 2. Presenter で Model と View をバインド
            new ScorePresenter().Initialize(scoreModel, _scoreView);
            new TimerPresenter().Initialize(timerModel, _timerView);
            new WeaponPresenter().Initialize(weaponModel, _weaponView);
            new GarbagePresenter().Initialize(garbageModel, _garbageView);

            // 3. GameTimer に TimerModel を注入
            _gameTimer.Initialize(timerModel);

            // 4. GarbageRegistry → GarbageTracker（ScoreModel 注入）
            var garbageRegistry = new GarbageRegistry(garbageModel);
            var registeredCount = 0;
            foreach (var g in _garbages)
            {
                if (g == null) continue;
                garbageRegistry.Register(g);
                registeredCount++;
            }
            garbageModel.SetInitialCount(registeredCount);
            new GarbageTracker(garbageModel, scoreModel).Initialize();

            // 5. WeaponController（WeaponModel 注入）
            _weaponController.Initialize(weaponModel, _playerLocomotion, _floorGrid, _weaponDataList);

            // 6. 住人（ScoreModel 注入）
            foreach (var mover in _residentMovers)
            {
                if (mover == null) continue;
                mover.Initialize(_playerLocomotion.transform);
                if (mover.TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.Initialize(scoreModel);
            }
        }
    }
}
