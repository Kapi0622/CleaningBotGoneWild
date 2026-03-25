using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Resident
{
    /// <summary>
    /// 武器への反応を担当する。
    /// OnHit() で吹き飛び + SubScore 加算、TriggerFlee() で逃走、TriggerAngry() で怒り状態に遷移する。
    /// ScoreModel は Startup.cs から Initialize() で注入する。
    /// </summary>
    [RequireComponent(typeof(ResidentMover), typeof(Rigidbody))]
    public class ResidentReactor : MonoBehaviour
    {
        [SerializeField] private int _subScoreValue = 50;

        private ResidentMover _mover;
        private Rigidbody     _rb;
        private ScoreModel    _scoreModel;

        private void Awake()
        {
            _mover = GetComponent<ResidentMover>();
            _rb    = GetComponent<Rigidbody>();
        }

        public void Initialize(ScoreModel scoreModel)
        {
            _scoreModel = scoreModel;
        }

        /// <summary>武器の爆発・衝撃で呼ばれる。吹き飛び + SubScore 加算。</summary>
        public void OnHit(Vector3 forceDirection, float force)
        {
            _rb.AddForce(forceDirection * force, ForceMode.Impulse);
            _scoreModel?.AddSubScore(_subScoreValue);
            _mover.SetState(ResidentState.Hit);
            Debug.Log($"[ResidentReactor] {name} hit! SubScore: {_scoreModel?.SubScore.Value ?? 0}");
        }

        /// <summary>近くで破壊武器が使用されたときに呼ばれる。プレイヤーから逃走する。</summary>
        public void TriggerFlee() => _mover.SetState(ResidentState.Flee);

        /// <summary>掃除機を向けられたときに呼ばれる。怒り状態（向き変え・移動停止）に遷移する。</summary>
        public void TriggerAngry() => _mover.SetState(ResidentState.Angry);
    }
}
