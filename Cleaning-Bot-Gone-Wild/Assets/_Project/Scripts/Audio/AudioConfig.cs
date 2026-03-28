using UnityEngine;

namespace CleaningBot.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "CleaningBot/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("BGM")]
        public AudioClip bgmClip;
        [Range(0f, 1f)] public float bgmVolume = 0.7f;

        [Header("UI SE（共通デフォルト）")]
        public AudioClip buttonClickSound;
    }
}
