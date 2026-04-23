using CleaningBot.Presenter;
using CleaningBot.View;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// TitleScene の DIコンテナ。new / Initialize / SerializeField参照のみ。ロジック禁止。
    /// </summary>
    public class TitleStartup : MonoBehaviour
    {
        [SerializeField] private TitleView _titleView;
        [SerializeField] private SceneTransition _sceneTransition;

        private void Awake()
        {
            new TitlePresenter().Initialize(_titleView, _sceneTransition, this.GetCancellationTokenOnDestroy());
        }
    }
}
