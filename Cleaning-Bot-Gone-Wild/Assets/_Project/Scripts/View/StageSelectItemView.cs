using CleaningBot.Data;
using CleaningBot.Stage;
using R3;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// ステージ選択一覧の個別アイテム。ステージ名・ロック状態・ベストランクを表示する。
    /// </summary>
    public class StageSelectItemView : MonoBehaviour
    {
        [SerializeField] private ClickEvent _button;
        [SerializeField] private TMP_Text   _stageNameText;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private TMP_Text   _bestRankText;

        private int _stageIndex;

        public Observable<int> OnClicked => _button.OnClicked.Select(_ => _stageIndex);

        public void Setup(int index, StageData data, bool isUnlocked, int bestRank)
        {
            _stageIndex = index;

            if (_stageNameText != null)
                _stageNameText.text = data.displayName;

            if (_lockIcon != null)
                _lockIcon.SetActive(!isUnlocked);

            if (_bestRankText != null)
                _bestRankText.text = bestRank > 0 ? new string('\u2605', bestRank) : string.Empty;

            _button.Interactable = isUnlocked;
        }
    }
}
