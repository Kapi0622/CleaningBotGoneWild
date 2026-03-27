using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Player;
using CleaningBot.Presenter;
using CleaningBot.Score;
using CleaningBot.Stage;
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
        [SerializeField] private GameSceneController _gameSceneController;

        [Header("Stage")]
        [SerializeField] private StageData _stageData;
        [SerializeField] private StageInitializer _stageInitializer;

        [Header("Views")]
        [SerializeField] private ScoreView _scoreView;
        [SerializeField] private TimerView _timerView;
        [SerializeField] private WeaponView _weaponView;
        [SerializeField] private GarbageView _garbageView;
        [SerializeField] private ResultView _resultView;

        private void Awake()
        {
            // StageLoader 経由で StageData を取得
            var stageLoader = new StageLoader(_stageData);
            var stageData   = stageLoader.Load();

            // 1. Model 生成
            var scoreModel   = new ScoreModel();
            var timerModel   = new TimerModel(stageData.timeLimit);
            var weaponModel  = new WeaponModel();
            var garbageModel = new GarbageModel();

            // 2. Presenter で Model と View をバインド
            new ScorePresenter().Initialize(scoreModel, _scoreView);
            new TimerPresenter().Initialize(timerModel, _timerView);
            new WeaponPresenter().Initialize(weaponModel, _weaponView);
            new GarbagePresenter().Initialize(garbageModel, _garbageView);

            // 3. GameTimer に TimerModel を注入し、GameStateController を生成
            _gameTimer.Initialize(timerModel);
            var gameStateController = new GameStateController(_gameTimer, timerModel, _playerLocomotion.transform);
            _gameSceneController.SetController(gameStateController);

            // 4. StageInitializer でゴミ・住人を生成し、GarbageTracker を初期化
            _stageInitializer.Initialize(stageData, garbageModel, scoreModel, _playerLocomotion.transform);
            var garbageTracker = new GarbageTracker(garbageModel, scoreModel, gameStateController);
            garbageTracker.Initialize();

            // 5. WeaponController（WeaponModel 注入）
            _weaponController.Initialize(weaponModel, _playerLocomotion, _floorGrid, stageData.weaponDataList);

            // 6. StageResetter を生成（リトライ処理の実体）
            var stageResetter = new StageResetter(
                scoreModel, timerModel, weaponModel, garbageModel,
                _floorGrid, _stageInitializer, garbageTracker,
                _playerLocomotion.transform, stageData, gameStateController);

            // 7. ResultPresenter（リトライ購読含む。必ず ChangeState より前に初期化する）
            new ResultPresenter().Initialize(
                gameStateController, scoreModel, garbageModel,
                timerModel, new RankCalculator(), stageData, stageResetter, _resultView);

            // 8. ゲーム開始（最終行）
            gameStateController.ChangeState(new InGameState());
        }
    }
}
