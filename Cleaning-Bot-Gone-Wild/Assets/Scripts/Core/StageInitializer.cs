using System.Collections.Generic;
using CleaningBot.Data;
using CleaningBot.Garbage;
using CleaningBot.Resident;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// ゴミと住人の生成・破棄・再生成ライフサイクルを管理する MonoBehaviour。
    /// Startup から Initialize() で依存を受け取り、SpawnAll() でシーンに配置する。
    /// StageResetter から ReInitialize() を呼ぶことでシーンリロードなしの再配置が行える。
    /// </summary>
    public class StageInitializer : MonoBehaviour
    {
        private StageData  _stageData;
        private GarbageModel _garbageModel;
        private ScoreModel   _scoreModel;
        private Transform    _playerTransform;

        private readonly List<GarbageBase>   _activeGarbages  = new();
        private readonly List<ResidentMover> _activeResidents = new();

        public void Initialize(StageData stageData, GarbageModel garbageModel,
                               ScoreModel scoreModel, Transform playerTransform)
        {
            _stageData       = stageData;
            _garbageModel    = garbageModel;
            _scoreModel      = scoreModel;
            _playerTransform = playerTransform;
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
                if (garbage == null) continue;
                registry.Register(garbage);
                _activeGarbages.Add(garbage);
            }
            _garbageModel.SetInitialCount(_activeGarbages.Count);

            for (var i = 0; i < _stageData.residentSpawnPoints.Count; i++)
            {
                if (_stageData.residentPrefab == null) continue;
                var go    = Instantiate(_stageData.residentPrefab, _stageData.residentSpawnPoints[i],
                                        Quaternion.identity, transform);
                var mover = go.GetComponent<ResidentMover>();
                if (mover == null) continue;
                mover.Initialize(_playerTransform);
                if (go.TryGetComponent<ResidentReactor>(out var reactor))
                    reactor.Initialize(_scoreModel);
                _activeResidents.Add(mover);
            }
        }

        private void DestroyAll()
        {
            foreach (var g in _activeGarbages)
                if (g != null) Destroy(g.gameObject);
            _activeGarbages.Clear();

            foreach (var r in _activeResidents)
                if (r != null) Destroy(r.gameObject);
            _activeResidents.Clear();
        }
    }
}
