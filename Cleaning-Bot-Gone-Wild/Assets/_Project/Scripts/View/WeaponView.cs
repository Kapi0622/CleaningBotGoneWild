using System.Threading;
using CleaningBot.Data;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CleaningBot.View
{
    /// <summary>
    /// 現在装備中の武器を表示する View。
    /// 切替時に ScaleX スピン + パンチスケール演出。
    /// </summary>
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _weaponNameText;

        private WeaponType _currentType;
        private bool _initialized;
        private CancellationTokenSource _spinCts;

        public void UpdateWeapon(WeaponType type)
        {
            var weaponName = type switch
            {
                WeaponType.Vacuum    => "掃除機",
                WeaponType.Rocket    => "ロケット",
                WeaponType.BlackHole => "ブラックホール",
                _                   => type.ToString(),
            };

            // 初回はアニメなしで即座に表示
            if (!_initialized)
            {
                _initialized = true;
                _currentType = type;
                _weaponNameText.text = weaponName;
                return;
            }

            // 同じ武器なら何もしない
            if (type == _currentType) return;
            _currentType = type;

            _spinCts?.Cancel();
            _spinCts?.Dispose();
            _spinCts = new CancellationTokenSource();
            PlaySpinAsync(weaponName, _spinCts.Token).Forget();
        }

        private async UniTaskVoid PlaySpinAsync(string weaponName, CancellationToken ct)
        {
            _weaponNameText.transform.localScale = Vector3.one;

            // ScaleX 1→0 (非表示へ)
            await LMotion.Create(1f, 0f, 0.1f)
                .WithEase(Ease.InQuad)
                .BindToLocalScaleX(_weaponNameText.transform)
                .ToUniTask(ct);

            _weaponNameText.text = weaponName;

            // ScaleX 0→1 (表示へ)
            await LMotion.Create(0f, 1f, 0.1f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScaleX(_weaponNameText.transform)
                .ToUniTask(ct);

            // パンチ
            await LMotion.Punch.Create(Vector3.one, Vector3.one * 0.2f, 0.2f)
                .WithFrequency(6)
                .BindToLocalScale(_weaponNameText.transform)
                .ToUniTask(ct);
        }

        private void OnDestroy()
        {
            _spinCts?.Cancel();
            _spinCts?.Dispose();
        }
    }
}
