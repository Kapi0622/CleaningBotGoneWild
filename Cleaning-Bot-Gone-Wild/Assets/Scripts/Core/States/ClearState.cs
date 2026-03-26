namespace CleaningBot.Core
{
    /// <summary>
    /// 全ゴミ除去によるクリア状態。
    /// OnEnter でタイマーを停止し、以降は何もしない。
    /// </summary>
    public class ClearState : IGameState
    {
        public void OnEnter(GameStateController ctx) => ctx.GameTimer.StopTimer();
        public void OnExit(GameStateController ctx) { }
        public void Update(GameStateController ctx) { }
    }
}
