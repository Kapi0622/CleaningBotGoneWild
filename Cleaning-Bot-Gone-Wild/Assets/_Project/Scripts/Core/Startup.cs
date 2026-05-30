using System.Collections.Generic;
using System.Threading;
using CleaningBot.Audio;
using CleaningBot.CameraSystem;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Player;
using CleaningBot.Presenter;
using CleaningBot.Score;
using CleaningBot.Stage;
using CleaningBot.View;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private List<FloorGrid> _floorGrids;
        [SerializeField] private GameTimer _gameTimer;
        [SerializeField] private GameSceneController _gameSceneController;
        [SerializeField] private Camera _mainCamera;

        [Header("Stage")]
        [SerializeField] private StageInitializer _stageInitializer;

        [Header("Stage Selection")]
        [SerializeField] private StageDatabase       _stageDatabase;
        [SerializeField] private SelectedStageHolder _selectedStageHolder;
        [SerializeField] private SceneTransition     _sceneTransition;

        [Header("Audio")]
        [SerializeField] private BgmPlayer _bgmPlayer;
        [SerializeField] private AudioConfig _audioConfig;

        [Header("Views")]
        [SerializeField] private ScoreView _scoreView;
        [SerializeField] private TimerView _timerView;
        [SerializeField] private WeaponView _weaponView;
        [SerializeField] private GarbageView _garbageView;
        [SerializeField] private ResultView _resultView;

        [Header("UI Animation Views")]
        [SerializeField] private FloatingScoreView _floatingScoreView;
        [SerializeField] private ScreenFadeView _screenFadeView;
        [SerializeField] private CountdownView _countdownView;

        [Header("Camera")]
        [SerializeField] private List<RoomBounds> _rooms;

        private void Awake()
        {
            // StageDatabase と SelectedStageHolder 経由で StageData を取得
            var stageData = _stageDatabase.GetStage(_selectedStageHolder.SelectedStageIndex);

            // 環境プレハブを生成し、FloorGrid / RoomBounds を自動収集（Inspector 未設定時のフォールバック）
            if (stageData.stageEnvironmentPrefab != null)
            {
                var env = Instantiate(stageData.stageEnvironmentPrefab);
                if (_floorGrids == null || _floorGrids.Count == 0 || _floorGrids.Exists(g => g == null))
                    _floorGrids = new List<FloorGrid>(env.GetComponentsInChildren<FloorGrid>(true));
                if (_rooms == null || _rooms.Count == 0 || _rooms[0] == null)
                    _rooms = new List<RoomBounds>(env.GetComponentsInChildren<RoomBounds>(true));
            }

            // Inspector 設定の検証（自動収集後）
            if (_floorGrids == null || _floorGrids.Count == 0 || _floorGrids.Exists(g => g == null))
            {
                Debug.LogError("[Startup] _floorGrids が未設定です。stageEnvironmentPrefab に FloorGrid を含むか、Inspector で設定してください。");
                enabled = false;
                return;
            }
            if (_rooms == null || _rooms.Count == 0 || _rooms[0] == null)
            {
                Debug.LogError("[Startup] _rooms が未設定です。stageEnvironmentPrefab に RoomBounds を含むか、Inspector で設定してください。");
                enabled = false;
                return;
            }

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

            // 2b. FloatingScorePresenter
            _floatingScoreView.Initialize(_mainCamera);
            new FloatingScorePresenter().Initialize(garbageModel, _floatingScoreView, _floatingScoreView);

            // 3. GameTimer に TimerModel を注入し、GameStateController を生成
            _gameTimer.Initialize(timerModel);
            var gameStateController = new GameStateController(_gameTimer, timerModel, _playerLocomotion.transform);
            _gameSceneController.SetController(gameStateController);

            // 4. PlayerRespawner を生成し GameStateController に注入
            var playerRespawner = new PlayerRespawner();
            playerRespawner.Initialize(_playerLocomotion, timerModel, stageData, stageData.playerStartPosition);
            gameStateController.SetPlayerRespawner(playerRespawner);

            // 4b. BonusGarbageSpawner を生成（FloorGrid は最初の部屋を使用）
            var bonusSpawner = new BonusGarbageSpawner();
            bonusSpawner.Initialize(_floorGrids[0], scoreModel, stageData, _playerLocomotion.transform);

            // 4c. StageInitializer でゴミ・住人を生成し、GarbageTracker を初期化
            _stageInitializer.Initialize(stageData, garbageModel, scoreModel, _playerLocomotion.transform);
            var garbageTracker = new GarbageTracker(
                garbageModel, scoreModel, gameStateController,
                timerModel, stageData, bonusSpawner);
            garbageTracker.Initialize();

            // 4b. 全 FloorGrid に ScoreModel を登録（床崩壊時の被害総額加算）
            foreach (var grid in _floorGrids) grid.RegisterScoreModel(scoreModel);

            // 5. WeaponController（WeaponModel 注入）
            _weaponController.Initialize(weaponModel, _playerLocomotion, _floorGrids, stageData.weaponDataList);

            // 6. CameraDirector を生成（部屋ごとカメラ切り替え）
            var startRoom      = _rooms[0];
            var cameraDirector = new CameraDirector(_rooms);
            cameraDirector.Initialize(startRoom);

            // 7. StageResetter を生成（リトライ処理の実体）
            var stageResetter = new StageResetter(
                scoreModel, timerModel, weaponModel, garbageModel,
                _floorGrids, _stageInitializer, garbageTracker,
                gameStateController,
                _weaponController, playerRespawner, bonusSpawner,
                _screenFadeView, _countdownView, _resultView, _timerView, _garbageView,
                cameraDirector, startRoom);

            // 8. ResultPresenter（リトライ購読含む。必ず ChangeState より前に初期化する）
            new ResultPresenter().Initialize(
                gameStateController, scoreModel, garbageModel,
                timerModel, new RankCalculator(), new TimeBonusCalculator(),
                stageData, stageResetter, _sceneTransition, _resultView,
                this.GetCancellationTokenOnDestroy());

            // 9. BGM 開始
            _bgmPlayer.Initialize(_audioConfig);

            // 10. フェードイン → カウントダウン → ゲーム開始
            StartGameAsync(gameStateController, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid StartGameAsync(GameStateController gameStateController, CancellationToken ct)
        {
            _screenFadeView.gameObject.SetActive(true);
            Time.timeScale = 0f;
            try
            {
                await _screenFadeView.FadeIn(0.5f, ct);
                await _countdownView.PlayCountdownAsync(ct);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return;
            }
            finally
            {
                Time.timeScale = 1f;
            }
            gameStateController.ChangeState(new InGameState());
        }
    }
}
