using UnityEngine;

namespace CleaningBot.Effects
{
    /// <summary>
    /// パーティクルエフェクトの再生ユーティリティ。
    ///
    /// PlayAt  : ワンショット再生。duration + startLifetime を元に自動破棄を予約する。
    ///           プレハブの Stop Action 設定に依存しないため、設定ミスによるオブジェクト残留を防ぐ。
    ///           Stop Action = Destroy が設定されていても二重破棄にはならない（Unity の Destroy は冪等）。
    ///
    /// PlayLoopAt : ループ再生。返された GameObject の Destroy は呼び出し元の責務。
    /// </summary>
    public static class ParticlePlayer
    {
        // ---- One-shot -------------------------------------------------------

        /// <summary>ワンショットエフェクトを pos に再生する。</summary>
        public static void PlayAt(ParticleSystem prefab, Vector3 pos)
            => PlayAt(prefab, pos, Quaternion.identity);

        /// <summary>ワンショットエフェクトを pos, rot に再生する。方向性のあるVFX向け。</summary>
        public static void PlayAt(ParticleSystem prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null) return;
            var instance = Object.Instantiate(prefab, pos, rot);
            instance.Play();
            ScheduleDestroy(instance);
        }

        // ---- Loop -----------------------------------------------------------

        /// <summary>ループエフェクトを pos に再生して GameObject を返す。呼び出し元が Destroy する。</summary>
        public static GameObject PlayLoopAt(ParticleSystem prefab, Vector3 pos)
            => PlayLoopAt(prefab, pos, Quaternion.identity);

        /// <summary>ループエフェクトを pos, rot に再生して GameObject を返す。呼び出し元が Destroy する。</summary>
        public static GameObject PlayLoopAt(ParticleSystem prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab == null) return null;
            var instance = Object.Instantiate(prefab, pos, rot);
            instance.Play();
            return instance.gameObject;
        }

        // ---- Internal -------------------------------------------------------

        /// <summary>
        /// duration + startLifetime の最大値から自動破棄タイミングを計算して Object.Destroy を予約する。
        /// ループ設定のプレハブには適用しない（ループは呼び出し元管理）。
        /// </summary>
        private static void ScheduleDestroy(ParticleSystem ps)
        {
            var main = ps.main;
            if (main.loop) return;

            // startLifetime がカーブモードの場合も constantMax で最大値を取得できる
            float maxLifetime   = main.startLifetime.constantMax;
            float destroyDelay  = main.duration + maxLifetime + 0.1f; // 0.1s は余裕バッファ
            Object.Destroy(ps.gameObject, destroyDelay);
        }
    }
}
