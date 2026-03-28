using R3;
using UnityEngine;
using CleaningBot.Score;

namespace CleaningBot.Core
{
    /// <summary>
    /// ゲーム状態を管理する純粋クラス。
    /// 状態遷移は ChangeState() のみ経由し、
    /// OnStateChanged で ResultPresenter など外部に通知する。
    /// Update() は GameSceneController（MonoBehaviour）から毎フレーム委譲される。
    /// </summary>
    public class GameStateController
    {
        public readonly GameTimer GameTimer;
        public readonly TimerModel TimerModel;
        public readonly Transform PlayerTransform;

        private readonly Subject<IGameState> _onStateChanged = new();
        public Observable<IGameState> OnStateChanged => _onStateChanged;

        public IGameState CurrentState => _current;

        private IGameState _current;

        public GameStateController(GameTimer gameTimer, TimerModel timerModel, Transform playerTransform)
        {
            GameTimer        = gameTimer;
            TimerModel       = timerModel;
            PlayerTransform  = playerTransform;
        }

        public void ChangeState(IGameState next)
        {
            _current?.OnExit(this);
            _current = next;
            _current.OnEnter(this);       // StopTimer / StartTimer はここで実行
            _onStateChanged.OnNext(next); // ResultPresenter はここで ResultData を組み立てる
        }

        public void Update() => _current?.Update(this);
    }
}
