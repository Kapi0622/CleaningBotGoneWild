using System.Threading;
using CleaningBot.Data;
using CleaningBot.Player;
using CleaningBot.Score;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 落下検知・残機カウント・リスポーン実行を担う純粋クラス。
    /// InGameState / BonusState の Update() から CheckAndHandleFall() を呼ばれる。
    /// </summary>
    public class PlayerRespawner
    {
        private const float FallThresholdY = -5f;

        private PlayerLocomotion _locomotion;
        private TimerModel _timerModel;
        private Vector3 _spawnPosition;
        private int _maxCoreFallCount;
        private float _respawnTimePenalty;

        private int _fallCount;
        private bool _isRespawning;
        private CancellationTokenSource _cts = new();

        public void Initialize(
            PlayerLocomotion locomotion,
            TimerModel timerModel,
            StageData stageData,
            Vector3 spawnPosition)
        {
            _locomotion          = locomotion;
            _timerModel          = timerModel;
            _spawnPosition       = spawnPosition;
            _maxCoreFallCount    = stageData.maxCoreFallCount;
            _respawnTimePenalty  = stageData.respawnTimePenalty;
        }

        /// <summary>
        /// 毎フレーム状態の Update() から呼ぶ。
        /// isCore=true のとき残機を消費し上限超過で FailState へ遷移する。
        /// isCore=false（ボーナスフェーズ）のときは時間ペナルティのみで Fail にならない。
        /// </summary>
        public void CheckAndHandleFall(GameStateController ctx, bool isCore)
        {
            if (_isRespawning) return;
            if (_locomotion.transform.position.y >= FallThresholdY) return;

            if (isCore)
            {
                _fallCount++;
                if (_fallCount > _maxCoreFallCount)
                {
                    ctx.ChangeState(new FailState());
                    return;
                }
            }
            else
            {
                _timerModel.SubtractTime(_respawnTimePenalty);
            }

            RespawnAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 速度ゼロ化込みでスポーン位置に瞬間移動する。
        /// StageResetter が Transform 直書きする代わりにここを呼ぶことで Rigidbody の残速を確実に消せる。
        /// </summary>
        public void TeleportToSpawn() => _locomotion.Teleport(_spawnPosition);

        /// <summary>リトライ時に呼ぶ。落下カウントをリセットし進行中のリスポーンをキャンセルする。</summary>
        public void Reset()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts       = new CancellationTokenSource();
            _fallCount = 0;
            _isRespawning = false;
        }

        private async UniTaskVoid RespawnAsync(CancellationToken ct)
        {
            _isRespawning = true;
            try
            {
                _locomotion.Teleport(_spawnPosition);
                await UniTask.Delay(500, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                _isRespawning = false;
            }
        }
    }
}
