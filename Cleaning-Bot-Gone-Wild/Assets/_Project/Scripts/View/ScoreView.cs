using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// スコアの UI 表示。
    /// メインスコア: バウンス + 黄色フラッシュ演出。
    /// </summary>
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _mainScoreText;

        private Color _mainDefaultColor;

        private void Awake()
        {
            _mainDefaultColor = _mainScoreText.color;
        }

        public void UpdateMainScore(int value)
        {
            _mainScoreText.text = value.ToString();

            // バウンス
            _mainScoreText.transform.localScale = Vector3.one;
            LMotion.Punch.Create(Vector3.one, Vector3.one * 0.3f, 0.2f)
                .WithFrequency(6)
                .BindToLocalScale(_mainScoreText.transform)
                .AddTo(this);

            // 黄色フラッシュ → デフォルト色に戻る
            var defaultColor = _mainDefaultColor;
            LMotion.Create(_mainScoreText.color, Color.yellow, 0.1f)
                .WithOnComplete(() =>
                    LMotion.Create(Color.yellow, defaultColor, 0.15f)
                        .BindToColor(_mainScoreText)
                        .AddTo(this))
                .BindToColor(_mainScoreText)
                .AddTo(this);
        }

    }
}
