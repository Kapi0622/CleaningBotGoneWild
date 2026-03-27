namespace CleaningBot.Core
{
    /// <summary>
    /// ゲームプレイ中の状態。
    /// OnEnter でタイマーを開始し、毎フレームタイムアップ・落下を監視して FailState へ遷移する。
    /// </summary>
    public class InGameState : IGameState
    {
        private const float FallThresholdY = -5f;

        public void OnEnter(GameStateController ctx) => ctx.GameTimer.StartTimer();

        public void OnExit(GameStateController ctx) { }

        public void Update(GameStateController ctx)
        {
            if (ctx.TimerModel.IsTimeUp || ctx.PlayerTransform.position.y < FallThresholdY)
                ctx.ChangeState(new FailState());
        }
    }
}
