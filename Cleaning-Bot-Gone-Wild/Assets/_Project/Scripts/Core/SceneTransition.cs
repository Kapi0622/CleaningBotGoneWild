using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleaningBot.Core
{
    /// <summary>
    /// フェードアウト → シーンロードを担う MonoBehaviour。各シーンにローカルに配置する。
    /// フェードインは行わない。新シーンの Awake で alpha=0 / blocksRaycasts=false に初期化される。
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadePanel;

        private const float FadeDuration = 0.5f;

        private void Awake()
        {
            if (_fadePanel == null) return;
            _fadePanel.alpha = 0f;
            _fadePanel.blocksRaycasts = false;
            _fadePanel.interactable   = false;
        }

        public async UniTask LoadSceneAsync(string sceneName, CancellationToken ct)
        {
            if (_fadePanel != null)
            {
                _fadePanel.blocksRaycasts = true;
                _fadePanel.interactable   = true;
                await LMotion.Create(0f, 1f, FadeDuration)
                    .BindToAlpha(_fadePanel)
                    .ToUniTask(ct);
            }

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);
        }
    }
}
