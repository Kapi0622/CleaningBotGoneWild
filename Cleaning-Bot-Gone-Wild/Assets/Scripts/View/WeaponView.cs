using TMPro;
using UnityEngine;
using CleaningBot.Data;

namespace CleaningBot.View
{
    /// <summary>
    /// 現在装備中の武器を表示する View。
    /// </summary>
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _weaponNameText;

        public void UpdateWeapon(WeaponType type)
        {
            _weaponNameText.text = type switch
            {
                WeaponType.Vacuum    => "掃除機",
                WeaponType.Rocket    => "ロケット",
                WeaponType.BlackHole => "ブラックホール",
                _                   => type.ToString(),
            };
        }
    }
}
