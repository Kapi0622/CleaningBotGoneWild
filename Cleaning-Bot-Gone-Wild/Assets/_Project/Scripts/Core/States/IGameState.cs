namespace CleaningBot.Core
{
    /// <summary>
    /// ゲーム状態のインターフェース。
    /// GameStateController がコンテキストとして渡され、状態遷移・タイマー制御を行う。
    /// </summary>
    public interface IGameState
    {
        void OnEnter(GameStateController ctx);
        void OnExit(GameStateController ctx);
        void Update(GameStateController ctx);
    }
}
