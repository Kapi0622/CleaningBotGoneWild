using UnityEngine;

namespace CleaningBot.Data
{
    public enum WeaponType { Vacuum, Rocket, BlackHole }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "CleaningBot/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public WeaponType weaponType;
        public float cooldown;       // クールタイム（秒）。Vacuum は 0
        public float floorDamage;    // 床へのダメージ量。Vacuum は 0
        public float blastRadius;    // 爆発・吸引の範囲。Vacuum は 0
        public AudioClip fireSound;      // 仮 SE 用。未設定時は無音
        public float residentHitForce = 8f; // 住人に加える吹き飛び力（Impulse）
        public float throwRange;             // 投擲水平距離（m）。BlackHole のみ使用

        [Header("Effects")]
        public ParticleSystem impactEffectPrefab;   // Rocket: 爆発(one-shot) / BlackHole: 渦(loop) / Vacuum: 吸引コーン(loop)
        public GameObject     indicatorPrefab;      // BlackHole 着地予測マーカー Prefab（未設定時は CreatePrimitive にフォールバック）
        public Material       trajectoryLineMaterial; // BlackHole 軌道ライン用マテリアル（未設定時はデフォルト）

        [Header("Camera")]
        public float shakeIntensity; // スクリーンシェイク強度。Vacuum=0, Rocket=0.3, BlackHole=0.8
    }
}
