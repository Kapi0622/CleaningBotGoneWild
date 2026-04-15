using System.Collections.Generic;
using CleaningBot.Data;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Stage
{
    /// <summary>
    /// ゴミと住人の生成・破棄・再生成ライフサイクルを管理する MonoBehaviour。
    /// Startup から Initialize() で依存を受け取り、SpawnAll() でシーンに配置する。
    /// StageResetter から ReInitialize() を呼ぶことでシーンリロードなしの再配置が行える。
    /// </summary>
    public class StageInitializer : MonoBehaviour
    {
        private StageData    _stageData;
        private IReadOnlyList<ResidentSpawnPoint> _residentSpawnPoints;
        private GarbageModel _garbageModel;
        private ScoreModel   _scoreModel;
        private Transform    _playerTransform;

        private readonly List<GarbageBase>   _activeGarbages  = new();
        private readonly List<ResidentMover> _activeResidents = new();

        public void Initialize(StageData stageData,
                               IReadOnlyList<ResidentSpawnPoint> residentSpawnPoints,
                               GarbageModel garbageModel,
                               ScoreModel scoreModel, Transform playerTransform)
        {
            _stageData           = stageData;
            _residentSpawnPoints = residentSpawnPoints;
            _garbageModel        = garbageModel;
            _scoreModel          = scoreModel;
            _playerTransform     = playerTransform;
            SpawnAll();
        }

        public void ReInitialize()
        {
            DestroyAll();
            SpawnAll();
        }

        private void SpawnAll()
        {
            var registry = new GarbageRegistry(_garbageModel);

            foreach (var spawn in _stageData.garbageSpawns)
            {
                if (spawn.prefab == null) continue;
                var go      = Instantiate(spawn.prefab, spawn.spawnPosition, Quaternion.identity, transform);
                var garbage = go.GetComponent<GarbageBase>();
                if (garbage == null)
                {
                    Debug.LogWarning($"[StageInitializer] prefab '{spawn.prefab.name}' に GarbageBase がありません。破棄します。", spawn.prefab);
                    Destroy(go);
                    continue;
                }
                registry.Register(garbage);
                _activeGarbages.Add(garbage);
            }
            _garbageModel.SetInitialCount(_activeGarbages.Count);

            var spawnCount = Mathf.Min(_stageData.residentCount, _residentSpawnPoints.Count);
            for (var i = 0; i < spawnCount; i++)
            {
                var spawnPoint = _residentSpawnPoints[i];
                var prefab     = spawnPoint.residentPrefabOverride != null
                    ? spawnPoint.residentPrefabOverride
                    : _stageData.residentPrefab;
                if (prefab == null) continue;

                var go    = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation, transform);
                var mover = go.GetComponent<ResidentMover>();
                if (mover == null)
                {
                    Debug.LogWarning($"[StageInitializer] prefab '{prefab.name}' に ResidentMover がありません。破棄します。", prefab);
                    Destroy(go);
                    continue;
                }
                mover.Initialize(_playerTransform);
                if (go.TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.Initialize(_scoreModel);
                _activeResidents.Add(mover);
            }
        }

        private void DestroyAll()
        {
            foreach (var g in _activeGarbages)
            {
                if (g == null) continue;
                g.gameObject.SetActive(false); // 同フレーム内の一時共存（コライダー等）を防ぐ
                Destroy(g.gameObject);
            }
            _activeGarbages.Clear();

            foreach (var r in _activeResidents)
            {
                if (r == null) continue;
                r.gameObject.SetActive(false);
                Destroy(r.gameObject);
            }
            _activeResidents.Clear();
        }
    }
}
