using UnityEngine;

namespace CleaningBot.Data
{
    /// <summary>
    /// シーン間でのステージ選択情報受け渡し用 ScriptableObject。
    /// DontDestroyOnLoad やシングルトンを使わずにシーン間データを共有する。
    /// </summary>
    [CreateAssetMenu(fileName = "SelectedStageHolder", menuName = "CleaningBot/SelectedStageHolder")]
    public class SelectedStageHolder : ScriptableObject
    {
        [SerializeField] private int _selectedStageIndex;

        public int SelectedStageIndex
        {
            get => _selectedStageIndex;
            set => _selectedStageIndex = value;
        }
    }
}
