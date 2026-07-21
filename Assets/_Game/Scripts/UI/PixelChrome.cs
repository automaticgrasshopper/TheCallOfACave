using UnityEngine;
using UnityEngine.UI;

namespace TCC.UI
{
    /// <summary>Reusable 9-slice-like pixel frame made from native UGUI images.
    /// It keeps the original control intact and adds sharp, non-raycastable chrome
    /// on top, so existing button bindings require no scene re-authoring.</summary>
    public class PixelChrome : MonoBehaviour
    {
        private const string FrameName = "[Pixel Chrome]";

        public static void Apply(GameObject target, Color primary, Color accent)
        {
            if (target.transform.Find(FrameName) != null) return;
            var chrome = target.AddComponent<PixelChrome>();
            chrome.Build(primary, accent);
        }

        private void Build(Color primary, Color accent)
        {
            var root = new GameObject(FrameName, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Outer rails plus broken corners give every panel a deliberate,
            // pixel-machined silhouette rather than Unity's default rounded box.
            Bar(root.transform, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -2), new Vector2(-18, 0), primary);
            Bar(root.transform, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(18, 0), new Vector2(-8, 2), primary);
            Bar(root.transform, "Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 8), new Vector2(2, -18), primary);
            Bar(root.transform, "Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-2, 18), new Vector2(0, -8), primary);
            Corner(root.transform, "TopLeft", new Vector2(5, -5), accent, -1, 1);
            Corner(root.transform, "TopRight", new Vector2(-5, -5), accent, 1, 1);
            Corner(root.transform, "BottomLeft", new Vector2(5, 5), accent, -1, -1);
            Corner(root.transform, "BottomRight", new Vector2(-5, 5), accent, 1, -1);
        }

        private static void Bar(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
        }

        private static void Corner(Transform parent, string name, Vector2 position, Color color, int x, int y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(x < 0 ? 0f : 1f, y < 0 ? 0f : 1f);
            rect.pivot = new Vector2(x < 0 ? 0f : 1f, y < 0 ? 0f : 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(8, 3);
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
        }
    }
}
