using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// GameStateController（純粋クラス）へ Unity の Update() を委譲する MonoBehaviour。
    /// Startup.cs が SetController() で参照を注入する。
    /// </summary>
    public class GameSceneController : MonoBehaviour
    {
        private GameStateController _stateController;

        public void SetController(GameStateController c) => _stateController = c;

        private void Update() => _stateController?.Update();
    }
}
