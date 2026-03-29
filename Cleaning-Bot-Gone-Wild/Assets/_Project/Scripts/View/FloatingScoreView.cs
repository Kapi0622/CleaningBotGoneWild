using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// ゴミ除去位置にスコアテキストをポップアップ表示する View。
    /// Prefab プール方式で TMP_Text を再利用する。
    /// Show() を呼ぶとワールド座標をスクリーン座標に変換して表示する。
    /// </summary>
    public class FloatingScoreView : MonoBehaviour
    {
        [SerializeField] private GameObject _floatingTextPrefab;
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private int _initialPoolSize = 5;

        private Camera _camera;
        private RectTransform _canvasRect;
        private readonly Queue<GameObject> _pool = new();
        private readonly Dictionary<GameObject, MotionHandle> _activeHandles = new();

        public void Initialize(Camera cam)
        {
            _camera = cam;
            _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            // 初期プール生成
            for (var i = 0; i < _initialPoolSize; i++)
                _pool.Enqueue(CreateInstance());
        }

        public void Show(Vector3 worldPos, int score)
        {
            var instance = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();

            // 前回アニメーションが残っていたらキャンセル
            if (_activeHandles.TryGetValue(instance, out var old) && old.IsActive())
                old.Cancel();

            instance.SetActive(true);

            var rect = instance.GetComponent<RectTransform>();
            var text = instance.GetComponent<TMP_Text>();
            var canvasGroup = instance.GetComponent<CanvasGroup>();

            // ワールド座標 → スクリーン座標 → Canvas ローカル座標
            var screenPos = _camera.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, null, out var localPos);
            rect.anchoredPosition = localPos;

            // テキスト設定
            text.text = $"+{score}";
            canvasGroup.alpha = 1f;
            rect.localScale = Vector3.zero;

            // 色分け
            text.color = score switch
            {
                >= 500 => new Color(1f, 0.84f, 0f),   // 金
                >= 200 => new Color(1f, 0.65f, 0f),    // オレンジ
                _      => Color.white,
            };

            // スケールバウンスイン → 上昇 + フェードアウト → プール返却
            var targetPos = new Vector2(localPos.x, localPos.y + 80f);

            var h1 = LMotion.Create(Vector3.zero, Vector3.one * 1.2f, 0.15f)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(rect);
            var h2 = LMotion.Create(Vector3.one * 1.2f, Vector3.one, 0.1f)
                .BindToLocalScale(rect);
            var h3 = LMotion.Create(localPos, targetPos, 0.8f)
                .WithEase(Ease.OutQuad)
                .BindToAnchoredPosition(rect);
            var h4 = LMotion.Create(1f, 0f, 0.4f)
                .BindToAlpha(canvasGroup);

            var seqHandle = LSequence.Create()
                .Append(h1)
                .Append(h2)
                .Join(h3)
                .Insert(0.5f, h4)
                .Run(b => b.WithOnComplete(() => ReturnToPool(instance)));

            _activeHandles[instance] = seqHandle;
        }

        private GameObject CreateInstance()
        {
            var go = Instantiate(_floatingTextPrefab, _poolRoot != null ? _poolRoot : transform);
            if (go.GetComponent<CanvasGroup>() == null)
                go.AddComponent<CanvasGroup>();
            go.SetActive(false);
            return go;
        }

        private void ReturnToPool(GameObject instance)
        {
            _activeHandles.Remove(instance);
            instance.SetActive(false);
            _pool.Enqueue(instance);
        }

        private void OnDestroy()
        {
            foreach (var handle in _activeHandles.Values)
                if (handle.IsActive()) handle.Cancel();
            _activeHandles.Clear();
        }
    }
}
