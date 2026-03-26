using R3;
using CleaningBot.Score;
using CleaningBot.View;

namespace CleaningBot.Presenter
{
    /// <summary>
    /// WeaponModel と WeaponView をバインドする Presenter。
    /// Subscribe と AddTo のみ。ビジネスロジックは持たない。
    /// </summary>
    public class WeaponPresenter
    {
        public void Initialize(WeaponModel model, WeaponView view)
        {
            model.CurrentWeapon
                .Subscribe(v => view.UpdateWeapon(v))
                .AddTo(view);
        }
    }
}
