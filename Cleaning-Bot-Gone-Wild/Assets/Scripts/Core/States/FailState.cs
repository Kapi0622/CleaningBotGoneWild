namespace CleaningBot.Core
{
    /// <summary>
    /// タイムアップによる失敗状態。
    /// OnEnter でタイマーを停止し、以降は何もしない。
    /// </summary>
    public class FailState : IGameState
    {
        public void OnEnter(GameStateController ctx) => ctx.GameTimer.StopTimer();
        public void OnExit(GameStateController ctx) { }
        public void Update(GameStateController ctx) { }
    }
}
