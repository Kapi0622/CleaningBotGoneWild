using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleaningBot.Core
{
    /// <summary>
    /// フェードアウト → シーンロード → フェードイン を担う MonoBehaviour。
    /// 各シーンにローカルに配置する（DontDestroyOnLoad しない）。
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadePanel;

        private const float FadeDuration = 0.5f;

        private void Awake()
        {
            if (_fadePanel != null)
                _fadePanel.alpha = 0f;
        }

        public async UniTask LoadSceneAsync(string sceneName, CancellationToken ct)
        {
            // フェードアウト（透明 → 不透明）
            if (_fadePanel != null)
            {
                await LMotion.Create(0f, 1f, FadeDuration)
                    .BindToAlpha(_fadePanel)
                    .ToUniTask(ct);
            }

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);

            // フェードインは新シーンの SceneTransition が担うため、ここでは行わない
        }
    }
}
