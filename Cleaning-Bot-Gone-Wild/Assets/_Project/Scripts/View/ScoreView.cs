using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// スコアの UI 表示。
    /// メインスコア: バウンス + 黄色フラッシュ演出。
    /// サブスコア: 右からスライドイン → フェードアウト演出。
    /// </summary>
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _mainScoreText;
        [SerializeField] private TMP_Text _subScoreText;

        private Color _mainDefaultColor;
        private CanvasGroup _subCanvasGroup;
        private RectTransform _subRect;
        private float _subOriginalX;
        private MotionHandle _subHandle;

        private void Awake()
        {
            _mainDefaultColor = _mainScoreText.color;

            _subRect = _subScoreText.GetComponent<RectTransform>();
            _subOriginalX = _subRect.anchoredPosition.x;

            // サブスコア用 CanvasGroup（フェード制御）
            _subCanvasGroup = _subScoreText.GetComponent<CanvasGroup>();
            if (_subCanvasGroup == null)
                _subCanvasGroup = _subScoreText.gameObject.AddComponent<CanvasGroup>();
            _subCanvasGroup.alpha = 0f;
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

        public void UpdateSubScore(int value)
        {
            _subScoreText.text = $"+{value}";

            // 前回アニメ停止 & 初期化
            if (_subHandle.IsActive()) _subHandle.Cancel();
            var pos = _subRect.anchoredPosition;
            pos.x = _subOriginalX + 60f; // 右オフセットから開始
            _subRect.anchoredPosition = pos;
            _subCanvasGroup.alpha = 0f;

            // スライドイン + フェードイン → 待機 → フェードアウト
            var targetPos = new Vector2(_subOriginalX, pos.y);
            _subHandle = LSequence.Create()
                .Append(LMotion.Create(pos, targetPos, 0.2f)
                    .WithEase(Ease.OutQuad)
                    .BindToAnchoredPosition(_subRect))
                .Join(LMotion.Create(0f, 1f, 0.15f)
                    .BindToAlpha(_subCanvasGroup))
                .AppendInterval(0.8f)
                .Append(LMotion.Create(1f, 0f, 0.3f)
                    .BindToAlpha(_subCanvasGroup))
                .Run();
        }

        private void OnDestroy()
        {
            if (_subHandle.IsActive()) _subHandle.Cancel();
        }
    }
}
