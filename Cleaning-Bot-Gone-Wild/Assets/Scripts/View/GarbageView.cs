using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 残りゴミ数の UI 表示。
    /// </summary>
    public class GarbageView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _garbageCountText;

        public void UpdateGarbageCount(int count)
            => _garbageCountText.text = $"残り {count} 個";
    }
}
