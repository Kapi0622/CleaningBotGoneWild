using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 残り時間の UI 表示。MM:SS 形式でフォーマットする。
    /// </summary>
    public class TimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        public void UpdateTimer(float remainingSeconds)
        {
            var minutes = (int)(remainingSeconds / 60);
            var seconds = (int)(remainingSeconds % 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
