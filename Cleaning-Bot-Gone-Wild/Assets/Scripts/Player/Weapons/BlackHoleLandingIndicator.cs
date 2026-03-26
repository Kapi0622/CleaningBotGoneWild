using System;
using UnityEngine;

namespace CleaningBot.Player.Weapons
{
    /// <summary>
    /// ブラックホール装備中に着地予測点を可視化するマーカー。
    /// BlackHoleStrategy の OnEquip() で生成・OnUnequip() で破棄される。
    /// </summary>
    public class BlackHoleLandingIndicator : MonoBehaviour
    {
        private Func<Vector3> _getLandingPoint;

        public void Initialize(Func<Vector3> getLandingPoint)
        {
            _getLandingPoint = getLandingPoint;
        }

        private void Update()
        {
            if (_getLandingPoint == null) return;
            transform.position = _getLandingPoint();
        }
    }
}
