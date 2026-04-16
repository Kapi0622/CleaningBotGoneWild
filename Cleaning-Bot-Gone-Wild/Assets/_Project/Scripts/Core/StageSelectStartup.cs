using CleaningBot.Data;
using CleaningBot.Presenter;
using CleaningBot.Stage;
using CleaningBot.View;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Core
{
    /// <summary>
    /// StageSelectScene の DIコンテナ。new / Initialize / SerializeField参照のみ。ロジック禁止。
    /// </summary>
    public class StageSelectStartup : MonoBehaviour
    {
        [SerializeField] private StageSelectView    _stageSelectView;
        [SerializeField] private SceneTransition    _sceneTransition;
        [SerializeField] private StageDatabase      _stageDatabase;
        [SerializeField] private SelectedStageHolder _selectedStageHolder;

        private void Awake()
        {
            var progress = new StageProgressStore();
            _stageSelectView.BuildItems(_stageDatabase, progress);
            new StageSelectPresenter().Initialize(
                _stageSelectView, _selectedStageHolder, _sceneTransition,
                this.GetCancellationTokenOnDestroy());
        }
    }
}
