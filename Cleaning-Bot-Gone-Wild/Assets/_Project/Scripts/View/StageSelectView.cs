using CleaningBot.Data;
using CleaningBot.Stage;
using R3;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// ステージ選択画面の View。アイテム一覧の生成とイベントの公開を担う。
    /// </summary>
    public class StageSelectView : MonoBehaviour
    {
        [SerializeField] private StageSelectItemView _itemPrefab;
        [SerializeField] private Transform           _itemParent;
        [SerializeField] private ClickEvent          _backToTitleButton;

        private readonly Subject<int> _onStageSelected = new();

        public Observable<int>  OnStageSelected => _onStageSelected;
        public Observable<Unit> OnBackClicked   => _backToTitleButton.OnClicked;

        public void BuildItems(StageDatabase db, StageProgressStore progress)
        {
            for (var i = 0; i < db.StageCount; i++)
            {
                var index     = i;
                var stageData = db.GetStage(index);
                var item      = Instantiate(_itemPrefab, _itemParent);
                item.Setup(index, stageData, progress.IsUnlocked(index), progress.GetBestRank(index));
                item.OnClicked
                    .Subscribe(idx => _onStageSelected.OnNext(idx))
                    .AddTo(this);
            }
        }

        private void OnDestroy()
        {
            _onStageSelected.Dispose();
        }
    }
}
