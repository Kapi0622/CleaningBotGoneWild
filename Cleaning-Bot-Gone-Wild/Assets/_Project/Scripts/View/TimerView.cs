using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 残り時間の UI 表示。MM:SS 形式でフォーマットする。
    /// SetWarning(true) で警告音を1回再生する（DistinctUntilChanged 済みのため重複なし）。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;

        [Header("Audio")]
        [SerializeField] private AudioClip _warningSound;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void UpdateTimer(float remainingSeconds)
        {
            var minutes = (int)(remainingSeconds / 60);
            var seconds = (int)(remainingSeconds % 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void SetWarning(bool isLow)
        {
            if (!isLow || _warningSound == null) return;
            _audioSource.PlayOneShot(_warningSound);
        }
    }
}
