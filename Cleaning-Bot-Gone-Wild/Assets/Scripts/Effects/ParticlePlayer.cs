using UnityEngine;

namespace CleaningBot.Effects
{
    /// <summary>
    /// パーティクルエフェクトの再生ユーティリティ。
    /// PlayAt: ワンショット再生（プレハブの Stop Action = Destroy が前提）。
    /// PlayLoopAt: ループ再生。返された GameObject の Destroy は呼び出し元の責務。
    /// </summary>
    public static class ParticlePlayer
    {
        /// <summary>
        /// ワンショットエフェクトを pos に再生する。
        /// prefab が null の場合は何もしない。
        /// プレハブの Stop Action が Destroy に設定されていること。
        /// </summary>
        public static void PlayAt(ParticleSystem prefab, Vector3 pos)
        {
            if (prefab == null) return;
            var instance = Object.Instantiate(prefab, pos, Quaternion.identity);
            instance.Play();
        }

        /// <summary>
        /// ループエフェクトを pos に再生して GameObject を返す。
        /// prefab が null の場合は null を返す。
        /// 呼び出し元が不要になったタイミングで Object.Destroy を呼ぶこと。
        /// </summary>
        public static GameObject PlayLoopAt(ParticleSystem prefab, Vector3 pos)
        {
            if (prefab == null) return null;
            var instance = Object.Instantiate(prefab, pos, Quaternion.identity);
            instance.Play();
            return instance.gameObject;
        }
    }
}
