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
    }
}
