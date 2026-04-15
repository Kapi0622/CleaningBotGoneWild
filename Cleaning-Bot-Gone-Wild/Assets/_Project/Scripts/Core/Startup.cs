using System.Threading;
using CleaningBot.Audio;
using CleaningBot.CameraSystem;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Player;
using CleaningBot.Presenter;
using CleaningBot.Resident;
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
        [SerializeField] private GameTimer _gameTimer;
        [SerializeField] private GameSceneController _gameSceneController;
        [SerializeField] private Camera _mainCamera;

        [Header("Stage")]
        [SerializeField] private StageData _stageData;
        [SerializeField] private StageInitializer _stageInitializer;

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

        private void Awake()
        {
            // StageLoader 経由で StageData を取得
            var stageLoader = new StageLoader(_stageData);
            var stageData   = stageLoader.Load();

            // Inspector 設定の早期検証
            if (stageData.stageEnvironmentPrefab == null)
            {
                Debug.LogError("[Startup] StageData.stageEnvironmentPrefab が未設定です。Inspector を確認してください。");
                enabled = false;
                return;
            }

            // 環境 Prefab をインスタンス化し、子コンポーネントを収集
            var stageEnv       = Instantiate(stageData.stageEnvironmentPrefab);
            var floorGrids     = stageEnv.GetComponentsInChildren<FloorGrid>();
            var rooms          = stageEnv.GetComponentsInChildren<RoomBounds>();
            var spawnPoints    = stageEnv.GetComponentsInChildren<ResidentSpawnPoint>();
            var playerSpawnT   = stageEnv.transform.Find("PlayerSpawn");
            var playerStartPos = playerSpawnT != null ? playerSpawnT.position : Vector3.zero;

            if (floorGrids.Length == 0)
            {
                Debug.LogError("[Startup] stageEnvironmentPrefab に FloorGrid が見つかりません。Prefab を確認してください。");
                enabled = false;
                return;
            }
            if (rooms.Length == 0)
            {
                Debug.LogError("[Startup] stageEnvironmentPrefab に RoomBounds が見つかりません。Prefab を確認してください。");
                enabled = false;
                return;
            }
            if (playerSpawnT == null)
            {
                Debug.LogWarning("[Startup] stageEnvironmentPrefab に 'PlayerSpawn' オブジェクトが見つかりません。原点 (0,0,0) を使用します。");
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

            // 4. StageInitializer でゴミ・住人を生成し、GarbageTracker を初期化
            _stageInitializer.Initialize(stageData, spawnPoints, garbageModel, scoreModel, _playerLocomotion.transform);
            var garbageTracker = new GarbageTracker(garbageModel, scoreModel, gameStateController);
            garbageTracker.Initialize();

            // 4b. 全 FloorGrid に ScoreModel を登録（床崩壊時の被害総額加算）
            foreach (var grid in floorGrids) grid.RegisterScoreModel(scoreModel);

            // 4c. プレイヤーを初期位置に配置
            _playerLocomotion.transform.position = playerStartPos;

            // 5. WeaponController（WeaponModel 注入）
            _weaponController.Initialize(weaponModel, _playerLocomotion, floorGrids, stageData.weaponDataList);

            // 6. CameraDirector を生成（部屋ごとカメラ切り替え）
            var startRoom      = rooms[0];
            var cameraDirector = new CameraDirector(rooms);
            cameraDirector.Initialize(startRoom);

            // 7. StageResetter を生成（リトライ処理の実体）
            var stageResetter = new StageResetter(
                scoreModel, timerModel, weaponModel, garbageModel,
                floorGrids, _stageInitializer, garbageTracker,
                _playerLocomotion.transform, playerStartPos, gameStateController,
                _weaponController,
                _screenFadeView, _countdownView, _resultView, _timerView, _garbageView,
                cameraDirector, startRoom);

            // 8. ResultPresenter（リトライ購読含む。必ず ChangeState より前に初期化する）
            new ResultPresenter().Initialize(
                gameStateController, scoreModel, garbageModel,
                timerModel, new RankCalculator(), new TimeBonusCalculator(),
                stageData, stageResetter, _resultView);

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
