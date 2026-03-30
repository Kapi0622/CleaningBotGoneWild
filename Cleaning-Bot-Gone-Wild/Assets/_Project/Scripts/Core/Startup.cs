using System.Threading;
using CleaningBot.Audio;
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
        [SerializeField] private FloorGrid _floorGrid;
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
            _stageInitializer.Initialize(stageData, garbageModel, scoreModel, _playerLocomotion.transform);
            var garbageTracker = new GarbageTracker(garbageModel, scoreModel, gameStateController);
            garbageTracker.Initialize();

            // 4b. FloorGrid に ScoreModel を登録（床崩壊時の被害総額加算）
            _floorGrid.RegisterScoreModel(scoreModel);

            // 5. WeaponController（WeaponModel 注入）
            _weaponController.Initialize(weaponModel, _playerLocomotion, _floorGrid, stageData.weaponDataList);

            // 6. StageResetter を生成（リトライ処理の実体）
            var stageResetter = new StageResetter(
                scoreModel, timerModel, weaponModel, garbageModel,
                _floorGrid, _stageInitializer, garbageTracker,
                _playerLocomotion.transform, stageData, gameStateController,
                _weaponController,
                _screenFadeView, _countdownView, _resultView, _timerView, _garbageView);

            // 7. ResultPresenter（リトライ購読含む。必ず ChangeState より前に初期化する）
            new ResultPresenter().Initialize(
                gameStateController, scoreModel, garbageModel,
                timerModel, new RankCalculator(), new TimeBonusCalculator(),
                stageData, stageResetter, _resultView);

            // 8. BGM 開始
            _bgmPlayer.Initialize(_audioConfig);

            // 9. フェードイン → カウントダウン → ゲーム開始
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
