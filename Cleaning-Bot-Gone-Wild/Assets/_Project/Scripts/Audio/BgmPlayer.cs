using UnityEngine;

namespace CleaningBot.Audio
{
    /// <summary>
    /// BGM のループ再生を担当する。
    /// Startup.cs から Initialize(AudioConfig) を呼んで再生開始する。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BgmPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
        }

        public void Initialize(AudioConfig config)
        {
            if (config == null || config.bgmClip == null) return;
            _audioSource.clip = config.bgmClip;
            _audioSource.volume = config.bgmVolume;
            _audioSource.Play();
        }

        /// <summary>将来の ResultPresenter 連携用。</summary>
        public void StopBgm() => _audioSource.Stop();
    }
}
