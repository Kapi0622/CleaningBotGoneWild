using UnityEngine;
using UnityEngine.UI;

namespace CleaningBot.Audio
{
    /// <summary>
    /// Canvas に1つ置くだけで、標準 Button を持つ全子 GameObject に共通SEを自動適用する。
    /// ClickEvent コンポーネントは自身で SE を制御するため、Button 経由では登録されない。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UiSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioConfig _audioConfig;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            foreach (var btn in GetComponentsInChildren<Button>(true))
                btn.onClick.AddListener(PlayDefaultSound);
        }

        /// <summary>
        /// 共通SE を再生する。ClickEvent からも _overrideClip 未設定時のフォールバックとして呼ばれる。
        /// </summary>
        public void PlayDefaultSound()
        {
            if (_audioConfig?.buttonClickSound != null)
                _audioSource.PlayOneShot(_audioConfig.buttonClickSound);
        }
    }
}
