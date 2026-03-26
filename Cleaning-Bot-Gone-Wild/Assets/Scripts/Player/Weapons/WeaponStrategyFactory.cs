using System;
using System.Collections.Generic;
using System.Linq;
using CleaningBot.Data;
using CleaningBot.Environment;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// 武器ストラテジーを生成・管理するファクトリー。
    /// Startup から注入され、WeaponController が Create() で Strategy を取得する。
    /// </summary>
    public class WeaponStrategyFactory
    {
        private readonly Dictionary<WeaponType, IWeaponStrategy> _strategies;

        public WeaponStrategyFactory(
            IReadOnlyList<WeaponData> dataList,
            FloorGrid floorGrid,
            Transform origin,
            AudioSource audioSource,
            Func<Vector3> getFacingDirection)
        {
            _strategies = new Dictionary<WeaponType, IWeaponStrategy>
            {
                {
                    WeaponType.Vacuum,
                    new VacuumStrategy(GetData(dataList, WeaponType.Vacuum), origin, audioSource)
                },
                {
                    WeaponType.Rocket,
                    new RocketStrategy(GetData(dataList, WeaponType.Rocket), floorGrid, origin, audioSource)
                },
                {
                    WeaponType.BlackHole,
                    new BlackHoleStrategy(
                        GetData(dataList, WeaponType.BlackHole), floorGrid, origin, audioSource, getFacingDirection)
                },
            };
        }

        public IWeaponStrategy Create(WeaponType type) => _strategies[type];

        private static WeaponData GetData(IReadOnlyList<WeaponData> list, WeaponType type)
            => list.First(d => d.weaponType == type);
    }
}
