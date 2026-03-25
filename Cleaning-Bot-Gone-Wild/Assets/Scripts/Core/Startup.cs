using System.Collections.Generic;
using CleaningBot.Data;
using CleaningBot.Environment;
using CleaningBot.Garbage;
using CleaningBot.Player;
using CleaningBot.Score;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// 依存関係の解決と初期化順序の管理のみ。
    /// ロジック・条件分岐・スコア計算は絶対に書かない。
    /// </summary>
    public class Startup : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion _playerLocomotion;
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private FloorGrid _floorGrid;
        [SerializeField] private List<WeaponData> _weaponDataList;
        [SerializeField] private List<GarbageBase> _garbages;

        private void Awake()
        {
            // Factory 生成・AudioSource 解決は WeaponController 内で完結。
            // Startup は AudioSource を知らない。
            _weaponController.Initialize(_playerLocomotion, _floorGrid, _weaponDataList);

            var garbageModel = new GarbageModel();
            var garbageRegistry = new GarbageRegistry(garbageModel);
            var registeredCount = 0;
            foreach (var g in _garbages)
            {
                if (g == null) continue;
                garbageRegistry.Register(g);
                registeredCount++;
            }
            garbageModel.SetInitialCount(registeredCount);
            var garbageTracker = new GarbageTracker(garbageModel);
            garbageTracker.Initialize();
        }
    }
}
