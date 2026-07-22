using UnityEngine;

namespace TCC.UI
{
    /// <summary>Shared hit test for world and inventory objects dragged onto the coin readout.</summary>
    public static class MoneyDropTarget
    {
        private static RectTransform _moneyRect;

        public static bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_moneyRect == null)
            {
                var money = GameObject.Find("Money");
                if (money != null) _moneyRect = money.transform as RectTransform;
            }
            return _moneyRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_moneyRect, screenPoint, null);
        }
    }
}
