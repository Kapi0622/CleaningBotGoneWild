using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// スコアの UI 表示。MonoBehaviour 継承により AddTo(view) が機能する。
    /// </summary>
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _mainScoreText;
        [SerializeField] private TMP_Text _subScoreText;

        public void UpdateMainScore(int value) => _mainScoreText.text = value.ToString();
        public void UpdateSubScore(int value)  => _subScoreText.text  = $"+{value}";
    }
}
