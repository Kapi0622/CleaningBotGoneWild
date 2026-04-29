namespace CleaningBot.Core
{
    /// <summary>
    /// コアゴミ全消去後のボーナスフェーズ状態。
    /// タイマーはそのまま継続し、ボーナスゴミが自動スポーンされる。
    /// 落下してもFailにならず、時間ペナルティのみ課される。
    /// タイムアップで ClearState へ遷移する。
    /// </summary>
    public class BonusState : IGameState
    {
        private readonly BonusGarbageSpawner _spawner;

        public BonusState(BonusGarbageSpawner spawner) => _spawner = spawner;

        public void OnEnter(GameStateController ctx) => _spawner.StartSpawning();

        public void OnExit(GameStateController ctx) => _spawner.StopSpawning();

        public void Update(GameStateController ctx)
        {
            if (ctx.TimerModel.IsTimeUp)
            {
                ctx.ChangeState(new ClearState());
                return;
            }

            ctx.PlayerRespawner.CheckAndHandleFall(ctx, isCore: false);
        }
    }
}
