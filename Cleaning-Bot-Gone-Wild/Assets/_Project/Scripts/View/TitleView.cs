using R3;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// タイトル画面の View。スタートボタンのクリックイベントを公開する。
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private ClickEvent _startButton;

        public Observable<Unit> OnStartClicked => _startButton.OnClicked;
    }
}
