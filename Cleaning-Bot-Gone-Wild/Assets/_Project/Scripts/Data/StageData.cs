using System;
using System.Collections.Generic;
using UnityEngine;

namespace CleaningBot.Data
{
    /// <summary>
    /// ステージの全パラメータを定義する ScriptableObject。
    /// コードを修正せずにエディタ上でステージを追加・調整できる。
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "CleaningBot/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("基本設定")]
        public string stageName;
        public float timeLimit = 180f;

        [Header("プレイヤー設定")]
        public Vector3 playerStartPosition;

        [Header("武器設定")]
        public List<WeaponType> availableWeapons;
        public List<WeaponData> weaponDataList;

        [Header("ゴミ設定")]
        public List<GarbageSpawnData> garbageSpawns;

        [Header("住人設定")]
        public GameObject residentPrefab;
        public int residentCount;
        public List<Vector3> residentSpawnPoints;

        [Header("スコア設定")]
        public int scoreStar2Threshold = 500;
        public int scoreStar3Threshold = 1000;
    }

    [Serializable]
    public class GarbageSpawnData
    {
        public GameObject prefab;
        public GarbageData garbageData;
        public Vector3 spawnPosition;
    }
}
