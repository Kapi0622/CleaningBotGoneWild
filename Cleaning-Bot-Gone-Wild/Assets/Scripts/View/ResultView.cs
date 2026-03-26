using R3;
using TMPro;
using UnityEngine;
using CleaningBot.Core;

namespace CleaningBot.View
{
    /// <summary>
    /// クリア・失敗パネルの表示切り替えと ResultData の内容描画を行う View。
    /// ResultPresenter から ShowClear / ShowFail / Hide が呼ばれる。
    /// リトライボタンは ClickEvent コンポーネント経由で OnRetryClicked として公開する。
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _clearPanel;
        [SerializeField] private GameObject _failPanel;

        [Header("Clear Panel Texts")]
        [SerializeField] private TMP_Text _clearMainScoreText;
        [SerializeField] private TMP_Text _clearRankText;
        [SerializeField] private TMP_Text _clearElapsedTimeText;

        [Header("Fail Panel Texts")]
        [SerializeField] private TMP_Text _failMainScoreText;
        [SerializeField] private TMP_Text _failRankText;

        [Header("Retry Buttons")]
        [SerializeField] private ClickEvent _clearRetryButton;
        [SerializeField] private ClickEvent _failRetryButton;

        private Observable<Unit> _onRetryClicked;
        public Observable<Unit> OnRetryClicked => _onRetryClicked ??= BuildOnRetryClicked();

        private Observable<Unit> BuildOnRetryClicked()
        {
            if (_clearRetryButton == null)
                throw new System.InvalidOperationException(
                    $"[ResultView] _clearRetryButton が未アサインです。{gameObject.name} の Inspector を確認してください。");
            if (_failRetryButton == null)
                throw new System.InvalidOperationException(
                    $"[ResultView] _failRetryButton が未アサインです。{gameObject.name} の Inspector を確認してください。");
            return Observable.Merge(_clearRetryButton.OnClicked, _failRetryButton.OnClicked);
        }

        private void Awake()
        {
            _clearPanel.SetActive(false);
            _failPanel.SetActive(false);
        }

        public void ShowClear(ResultData data)
        {
            _failPanel.SetActive(false);

            _clearMainScoreText.text   = $"スコア: {data.MainScore}";
            _clearRankText.text        = $"{new string('★', data.StarRank)}{new string('☆', 3 - data.StarRank)}";
            _clearElapsedTimeText.text = $"タイム: {FormatTime(data.ElapsedTime)}";

            _clearPanel.SetActive(true);
        }

        public void ShowFail(ResultData data)
        {
            _clearPanel.SetActive(false);

            _failMainScoreText.text = $"スコア: {data.MainScore}";
            _failRankText.text      = $"{new string('★', data.StarRank)}{new string('☆', 3 - data.StarRank)}";

            _failPanel.SetActive(true);
        }

        public void Hide()
        {
            _clearPanel.SetActive(false);
            _failPanel.SetActive(false);
        }

        private static string FormatTime(float seconds)
        {
            var m = (int)(seconds / 60);
            var s = (int)(seconds % 60);
            return $"{m:00}:{s:00}";
        }
    }
}
