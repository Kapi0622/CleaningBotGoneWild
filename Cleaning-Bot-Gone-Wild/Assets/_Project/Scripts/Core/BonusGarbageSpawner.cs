using System.Collections.Generic;
using System.Threading;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Score;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// ボーナスフェーズ中にランダムゴミをスポーンし続ける純粋クラス。
    /// BonusState.OnEnter/OnExit から StartSpawning/StopSpawning を呼ばれる。
    /// ゴミ除去スコアは GarbageModel を経由せず ScoreModel.AddMainScore() に直接加算する
    /// （コアゴミのカウントに影響させないため）。
    /// </summary>
    public class BonusGarbageSpawner
    {
        private FloorGrid _floorGrid;
        private ScoreModel _scoreModel;
        private StageData _stageData;
        private Transform _playerTransform;

        private int _activeCount;
        private readonly List<GarbageBase> _activeGarbages = new();
        private CancellationTokenSource _cts;

        public void Initialize(
            FloorGrid floorGrid,
            ScoreModel scoreModel,
            StageData stageData,
            Transform playerTransform)
        {
            _floorGrid       = floorGrid;
            _scoreModel      = scoreModel;
            _stageData       = stageData;
            _playerTransform = playerTransform;
        }

        /// <summary>BonusState.OnEnter から呼ぶ。スポーンループを開始する。</summary>
        public void StartSpawning()
        {
            StopSpawning();   // 旧ゴミ破棄・カウントリセットを StopSpawning に一本化（再入安全）
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// BonusState.OnExit から呼ぶ。スポーンループを停止し残存ゴミを全破棄する。
        /// リトライ時も StageResetter から呼ばれる。
        /// </summary>
        public void StopSpawning()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            foreach (var g in _activeGarbages)
            {
                if (g != null) Object.Destroy(g.gameObject);
            }
            _activeGarbages.Clear();
            _activeCount = 0;
        }

        private async UniTaskVoid SpawnLoopAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    var intervalMs = Mathf.Max(100, Mathf.RoundToInt(_stageData.bonusSpawnInterval * 1000f));
                    await UniTask.Delay(intervalMs, cancellationToken: ct);

                    if (_activeCount < _stageData.bonusMaxActiveCount)
                        SpawnOne();
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private void SpawnOne()
        {
            if (_stageData.bonusGarbagePrefab == null) return;

            var pos = _floorGrid.GetRandomAlivePosition(3f, _playerTransform.position);
            if (!pos.HasValue) return;

            var go = Object.Instantiate(_stageData.bonusGarbagePrefab, pos.Value, Quaternion.identity);
            var garbage = go.GetComponent<GarbageBase>();
            if (garbage == null)
            {
                Object.Destroy(go);
                return;
            }

            _activeCount++;
            _activeGarbages.Add(garbage);

            garbage.OnRemoved
                .Subscribe(g =>
                {
                    _activeCount--;
                    _activeGarbages.Remove(g);
                    _scoreModel.AddMainScore(g.Data?.scoreValue ?? 10);
                })
                .AddTo(garbage);
        }
    }
}
