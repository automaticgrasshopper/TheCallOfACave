using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>Turns the prototype UI into a crisp pixel-tech interface and draws
    /// readable world markers over the nursery and extractor work site.</summary>
    public class CaveArtDirector : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private SimulationManager _simulation;
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private RectTransform _hudCard;
        [SerializeField] private RectTransform _money;
        [SerializeField] private RectTransform _population;
        [SerializeField] private RectTransform _buyButton;
        [SerializeField] private RectTransform _pauseButton;
        [SerializeField] private RectTransform _languageButton;

        // World presentation is baked into Main.unity by ScenePresentationBaker.
        // This component is an editor recipe and never generates the world at runtime.

        /// <summary>Editor baker entry point. UI presentation is authored into the
        /// scene once, so edit mode and play mode share the exact same objects.</summary>
        public void ApplyScenePresentation()
        {
            RefineUILayout();
            RefineTitleLayout();
            StyleInterface();
        }

        private void InstallBackdrop()
        {
            if (GameObject.Find("CaveBackDrop") != null || Camera.main == null) return;
            Texture2D texture = MakeBarrenGround();

            var go = new GameObject("CaveBackDrop", typeof(SpriteRenderer));
            if (_worldRoot != null) go.transform.SetParent(_worldRoot, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            renderer.color = Color.white;

            Camera cam = Camera.main;
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            float scale = Mathf.Max(width / sprite.bounds.size.x, height / sprite.bounds.size.y);
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);
            go.transform.localScale = Vector3.one * scale;
        }

        private static Texture2D MakeBarrenGround()
        {
            const int w = 256, h = 144;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var sand = new Color(.27f, .20f, .12f);
            var dark = new Color(.035f, .045f, .048f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                bool rock = edge < 5 + ((x * 13 + y * 7) % 4);
                float grit = ((x * 17 + y * 31) % 11 == 0) ? .018f : 0f;
                tex.SetPixel(x, y, rock ? dark : sand + new Color(grit, grit * .65f, grit * .25f));
            }
            tex.Apply();
            return tex;
        }

        private void RefineUILayout()
        {
            var hud = _hudCard;
            if (hud != null)
            {
                bool inSidebar = hud.parent != null && hud.parent.name == "Right Command Sidebar";
                hud.sizeDelta = new Vector2(inSidebar ? 386f : 350f, inSidebar ? 286f : 292f);
                hud.anchoredPosition = new Vector2(inSidebar ? 0f : -18f, 18f);
            }
            SetRect(_money, new Vector2(0f, -17f), new Vector2(318f, 40f), 28f);
            SetRect(_population, new Vector2(0f, -60f), new Vector2(318f, 32f), 20f);
            SetRect(_buyButton, new Vector2(0f, -126f), new Vector2(254f, 46f), 20f);
            SetRect(_pauseButton, new Vector2(-77f, 18f), new Vector2(142f, 45f), 20f);
            SetRect(_languageButton, new Vector2(77f, 18f), new Vector2(142f, 45f), 20f);
        }

        private static void SetRect(RectTransform rect, Vector2 pos, Vector2 size, float fontSize)
        {
            if (rect == null) return;
            var go = rect.gameObject;
            if (rect != null) { rect.anchoredPosition = pos; rect.sizeDelta = size; }
            var text = go.GetComponent<TMP_Text>();
            if (text != null) text.fontSize = fontSize;
            foreach (var childText in go.GetComponentsInChildren<TMP_Text>(true)) childText.fontSize = fontSize;
        }

        private static void RefineTitleLayout()
        {
            var canvas = GameObject.Find("TitleMenuCanvas");
            if (canvas == null) return;
            if (canvas.transform.Find("Title Artwork") != null) return;
            var rects = canvas.GetComponentsInChildren<RectTransform>(true);
            RectTransform Find(string objectName)
            {
                foreach (var rect in rects) if (rect.name == objectName) return rect;
                return null;
            }

            var box = Find("Box");
            if (box != null)
            {
                box.anchoredPosition = Vector2.zero;
                box.sizeDelta = new Vector2(520f, 520f);
            }
            SetRect(Find("Title"), new Vector2(0f, 184f), new Vector2(460f, 72f), 48f);
            SetRect(Find("StartButton"), new Vector2(0f, 68f), new Vector2(360f, 60f), 24f);
            SetRect(Find("ContinueButton"), new Vector2(0f, -2f), new Vector2(360f, 60f), 24f);
            SetRect(Find("SettingsButton"), new Vector2(0f, -72f), new Vector2(360f, 60f), 24f);
            SetRect(Find("QuitButton"), new Vector2(0f, -142f), new Vector2(360f, 60f), 24f);

            var dim = Find("Dim")?.GetComponent<Image>();
            if (dim != null) dim.color = new Color(.018f, .022f, .024f, .92f);
            if (box != null && box.TryGetComponent<Image>(out var boxImage))
                boxImage.color = new Color(.025f, .032f, .035f, .98f);

            TintButton(Find("StartButton"), new Color(.13f, .19f, .17f, 1f));
            TintButton(Find("ContinueButton"), new Color(.07f, .105f, .11f, 1f));
            TintButton(Find("SettingsButton"), new Color(.07f, .105f, .11f, 1f));
            TintButton(Find("QuitButton"), new Color(.17f, .09f, .085f, 1f));
        }

        private static void TintButton(RectTransform rect, Color color)
        {
            if (rect != null && rect.TryGetComponent<Image>(out var image)) image.color = color;
        }

        private void StyleInterface()
        {
            foreach (Image image in FindObjectsOfType<Image>(true))
            {
                // PixelChrome may rebuild generated child rails while this edit-mode
                // snapshot is traversed, leaving destroyed entries in the array.
                if (image == null) continue;
                if (image.name.Contains("Dim") || image.name.Contains("Curtain")) continue;
                if (image.GetComponent<Button>() != null)
                {
                    image.color = new Color(.035f, .045f, .05f, .98f);
                    AddOutline(image.gameObject, new Color(.015f, .018f, .02f, .95f));
                    PixelChrome.Apply(image.gameObject, new Color(.42f, .37f, .27f), new Color(.72f, .55f, .27f));
                    var colors = image.GetComponent<Button>().colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1.12f, 1.05f, .86f, 1f);
                    colors.pressedColor = new Color(.82f, .72f, .52f, 1f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.colorMultiplier = 1f;
                    image.GetComponent<Button>().colors = colors;
                }
                else if (image.name == "Box" || image.name == "HudCard" || image.name == "ColonyLostCard")
                {
                    image.color = new Color(.025f, .032f, .037f, .95f);
                    PixelChrome.Apply(image.gameObject, new Color(.32f, .34f, .32f), new Color(.64f, .49f, .25f));
                }
            }

            foreach (TMP_Text text in FindObjectsOfType<TMP_Text>(true))
            {
                if (text.name == "Title") { text.color = new Color(.88f, .74f, .45f); text.characterSpacing = 2f; }
                else if (text.name == "Label") { text.color = new Color(.78f, .8f, .76f); text.characterSpacing = 0f; }
                if (text is TextMeshProUGUI && text.GetComponent<Shadow>() == null)
                {
                    var shadow = text.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, .55f);
                    shadow.effectDistance = new Vector2(1f, -1f);
                }
            }
        }

        private static void AddOutline(GameObject go, Color color)
        {
            if (go.GetComponent<Outline>() != null) return;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1, -1);
        }

        // ------------------------ world sites ------------------------
        private readonly System.Collections.Generic.List<SiteLabel> _siteLabels =
            new System.Collections.Generic.List<SiteLabel>();

        private void InstallSiteVisuals()
        {
            if (GameObject.Find("[PIXEL SITES]") != null) return;
            var sim = _simulation != null ? _simulation : (SimulationManager.Exists ? SimulationManager.Instance : null);
            if (sim == null) return;
            var root = new GameObject("[PIXEL SITES]");
            if (_worldRoot != null) root.transform.SetParent(_worldRoot, false);
            CreateSite(root.transform, "NurserySite", sim.BirthCenterPosition, 1.7f,
                new Color(.22f, .4f, .36f, .18f), new Color(.46f, .66f, .57f),
                LocalizationTable.Keys.ZoneNursery, false);
            if (sim.Labor != null)
                CreateSite(root.transform, "ExtractorSite", sim.Labor.Center, sim.Labor.Radius,
                    new Color(.45f, .31f, .16f, .18f), new Color(.7f, .52f, .28f),
                    LocalizationTable.Keys.ZoneFactory, true);
            var barracksSite = CreateSite(root.transform, "BarracksSite", new Vector2(-4.85f, -3.35f), 1.3f,
                new Color(.18f, .25f, .29f, .18f), new Color(.42f, .58f, .62f),
                LocalizationTable.Keys.ZoneBarracks, true);
            var barracks = barracksSite.AddComponent<TCC.Gameplay.BarracksZone>();
            barracks.Configure(1.3f, sim.Config.soldierTrainingSeconds);
            sim.RegisterBarracks(barracks);
            RefreshSiteLabels();
        }

        private GameObject CreateSite(Transform parent, string name, Vector2 position, float radius,
            Color fill, Color edge, string localizationKey, bool factory)
        {
            var site = new GameObject(name); site.transform.SetParent(parent);
            site.transform.position = position;
            var ring = site.AddComponent<SpriteRenderer>();
            ring.sprite = MakeSiteSprite(factory);
            ring.color = fill;
            ring.sortingOrder = -10;
            float visualScale = radius * .52f;
            site.transform.localScale = Vector3.one * visualScale;

            var edgeGo = new GameObject("Pixel Boundary", typeof(SpriteRenderer));
            edgeGo.transform.SetParent(site.transform, false);
            var edgeRenderer = edgeGo.GetComponent<SpriteRenderer>();
            edgeRenderer.sprite = MakeSiteSprite(factory);
            edgeRenderer.color = edge;
            edgeRenderer.sortingOrder = -9;
            edgeGo.transform.localScale = Vector3.one * 1.035f;

            // A second smaller translucent layer preserves a clear play area while
            // making the site feel like a constructed machine rather than a circle.
            var coreGo = new GameObject("Site Core", typeof(SpriteRenderer));
            coreGo.transform.SetParent(site.transform, false);
            var core = coreGo.GetComponent<SpriteRenderer>();
            core.sprite = MakeCoreSprite(factory); core.color = fill;
            core.sortingOrder = -8; coreGo.transform.localScale = Vector3.one * .85f;

            TMP_Text source = FindObjectOfType<TMP_Text>();
            if (source == null) return site;
            var labelGo = new GameObject("Site Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.position = position + Vector2.up * (visualScale + .28f);
            labelGo.transform.localScale = Vector3.one * .22f;
            var label = labelGo.GetComponent<TextMeshPro>();
            label.font = source.font; label.fontSize = 3.1f; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.72f, .72f, .65f);
            // World-space TMP outline setters access Renderer.material and create
            // scene-local material instances while the editor baker is running.
            if (label.font != null) label.fontSharedMaterial = label.font.material;
            label.enableWordWrapping = false; label.sortingOrder = 20;
            _siteLabels.Add(new SiteLabel { text = label, key = localizationKey });
            return site;
        }

        private void RefreshSiteLabels()
        {
            if (!LocalizationManager.Exists) return;
            foreach (var label in _siteLabels)
                if (label.text != null) label.text.text = LocalizationManager.Instance.Get(label.key);
        }

        private static Sprite MakeSiteSprite(bool factory)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) texture.SetPixel(x, y, clear);
            var center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x, y) - center;
                float dist = d.magnitude;
                bool ring = dist > 27f && dist < 29f;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 180f;
                bool ticks = dist > 24f && dist < 27f && Mathf.Repeat(angle, 45f) < 5f;
                if (ring || ticks) texture.SetPixel(x, y, Color.white);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 32f);
        }

        private static Sprite MakeCoreSprite(bool factory)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x - 31.5f, y - 31.5f);
                bool shape = factory
                    ? (Mathf.Abs(d.x) < 10 && Mathf.Abs(d.y) < 10 && (Mathf.Abs(d.x) > 7 || Mathf.Abs(d.y) > 7))
                    : (d.sqrMagnitude < 9 * 9 && d.sqrMagnitude > 6 * 6);
                texture.SetPixel(x, y, shape ? Color.white : clear);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 32f);
        }

        private struct SiteLabel { public TMP_Text text; public string key; }
    }
}
