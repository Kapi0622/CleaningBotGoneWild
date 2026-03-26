using R3;
using CleaningBot.Data;

namespace CleaningBot.Score
{
    /// <summary>
    /// 現在装備中の武器を保持する Model。
    /// WeaponController と WeaponPresenter が同一インスタンスを共有する（Startup.cs で管理）。
    /// </summary>
    public class WeaponModel
    {
        public readonly ReactiveProperty<WeaponType> CurrentWeapon = new(WeaponType.Vacuum);

        public void Reset() => CurrentWeapon.Value = WeaponType.Vacuum;
    }
}
