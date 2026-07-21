#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TCC.Data;
using TCC.Gameplay;
using TCC.Managers;
using TCC.UI;

namespace TCC.EditorTools
{
    /// <summary>Bakes final UI and world presentation into Main.unity. Only moving
    /// entities remain prefab-instantiated; every environment layer is selectable
    /// and editable in the normal Unity hierarchy.</summary>
    [InitializeOnLoad]
    public static class ScenePresentationBaker
    {
        private const string WorldArt = "Assets/_Game/Art/World/";

        static ScenePresentationBaker() => EditorApplication.delayCall += BakeOpenMainScene;

        [MenuItem("TCC/Bake Scene Presentation")]
        public static void BakeOpenMainScene()
        {
            if (Application.isPlaying) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/_Game/Scenes/Main.unity") return;

            BakeWorld();
            BakeEntityPrefabs();
            BakeHudExtensions();

            var director = Object.FindObjectOfType<CaveArtDirector>();
            if (director != null)
            {
                director.ApplyScenePresentation();
                EditorUtility.SetDirty(director);
            }

            foreach (var text in Object.FindObjectsOfType<LocalizedText>(true))
            {
                text.RefreshNow();
                EditorUtility.SetDirty(text);
                EditorUtility.SetDirty(text.GetComponent<TMP_Text>());
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TCC] Scene presentation baked: UI and world are now WYSIWYG.");
        }

        private static void BakeWorld()
        {
            Directory.CreateDirectory(WorldArt);
            var ground = EnsureSprite("barren_ground.png", MakeGround(), 16f);
            var ring = EnsureSprite("site_ring.png", MakeRing(), 32f);
            var pixel = EnsureSprite("white_pixel.png", MakePixel(), 4f);

            var world = GameObject.Find("[World]")?.transform;
            if (world == null) return;

            var backdrop = Child(world, "Cave Backdrop");
            var backdropRenderer = Component<SpriteRenderer>(backdrop);
            backdropRenderer.sprite = ground;
            backdropRenderer.color = Color.white;
            backdropRenderer.sortingOrder = -100;
            backdrop.transform.localPosition = Vector3.zero;
            backdrop.transform.localScale = Vector3.one;

            var birth = GameObject.Find("BirthCircle");
            var labor = GameObject.Find("LaborCircle");
            StyleSite(birth, ring, new Color(.33f, .56f, .48f, .5f), false);
            if (labor != null) labor.transform.localPosition = new Vector3(-7f, 3.2f, 0f);
            StyleSite(labor, ring, new Color(.68f, .48f, .25f, .24f), true);
            MakeLabel(world, "Nursery Label", new Vector3(-6.8f, -2.05f, 0f),
                LocalizationTable.Keys.ZoneNursery);
            MakeLabel(world, "Factory Label", new Vector3(-7f, 4.35f, 0f),
                LocalizationTable.Keys.ZoneFactory);
            var oldRiver = GameObject.Find("Sand River");
            if (oldRiver != null) Object.DestroyImmediate(oldRiver);
            var oldPit = GameObject.Find("Pit");
            if (oldPit != null) Object.DestroyImmediate(oldPit);
            var oldBarracks = GameObject.Find("BarracksSite");
            if (oldBarracks != null) Object.DestroyImmediate(oldBarracks);
            var oldLabor = labor != null ? labor.GetComponent<LaborZone>() : null;
            if (oldLabor != null) oldLabor.enabled = false;

            BakeFacility(world, labor, FacilityType.Factory, new Vector2(-7f, 3.2f), ring, pixel,
                LocalizationTable.Keys.ZoneFactory, "Factory Label");
            BakeFacility(world, null, FacilityType.Barracks, new Vector2(-4.55f, 3.2f), ring, pixel,
                LocalizationTable.Keys.ZoneBarracks, "Barracks Label");
            BakeFacility(world, null, FacilityType.Hospital, new Vector2(-4.55f, -3.2f), ring, pixel,
                LocalizationTable.Keys.ZoneHospital, "Hospital Label");
            BakeFacility(world, null, FacilityType.Academy, new Vector2(-2.1f, 3.2f), ring, pixel,
                LocalizationTable.Keys.ZoneAcademy, "Academy Label");
        }

        private static void BakeFacility(Transform world, GameObject existing, FacilityType type,
            Vector2 position, Sprite ring, Sprite pixel, string labelKey, string labelName)
        {
            var site = existing != null ? existing : Child(world, type + " Site");
            site.name = type + " Site";
            site.transform.localPosition = position;
            site.transform.localScale = Vector3.one * .82f;
            var ringRenderer = Component<SpriteRenderer>(site);
            ringRenderer.sprite = ring;
            ringRenderer.sortingOrder = -10;
            var collider = Component<CircleCollider2D>(site);
            collider.radius = 1.28f;

            var core = Child(site.transform, "Facility Core");
            core.transform.localPosition = Vector3.zero;
            var coreRenderer = Component<SpriteRenderer>(core);
            coreRenderer.sprite = ring;
            coreRenderer.sortingOrder = -9;

            var back = Child(site.transform, "Facility Progress Back");
            back.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            back.transform.localScale = new Vector3(1.8f, .17f, 1f);
            var backRenderer = Component<SpriteRenderer>(back);
            backRenderer.sprite = pixel;
            backRenderer.color = new Color(.015f, .03f, .04f, .95f);
            backRenderer.sortingOrder = 25;
            backRenderer.enabled = false;
            var fill = Child(back.transform, "Facility Progress Fill");
            fill.transform.localPosition = new Vector3(-.5f, 0f, -.01f);
            fill.transform.localScale = new Vector3(.01f, .55f, 1f);
            var fillRenderer = Component<SpriteRenderer>(fill);
            fillRenderer.sprite = pixel;
            fillRenderer.color = new Color(.35f, .75f, .62f, 1f);
            fillRenderer.sortingOrder = 26;
            fillRenderer.enabled = false;

            var facility = Component<ColonyFacility>(site);
            facility.Configure(type, 1.05f, ringRenderer, coreRenderer, backRenderer, fillRenderer);
            MakeLabel(world, labelName, new Vector3(position.x, position.y + 1.18f, 0f), labelKey);
        }

        private static void BakeHudExtensions()
        {
            var hud = Object.FindObjectOfType<HudView>(true);
            var card = GameObject.Find("HudCard")?.transform as RectTransform;
            var buy = GameObject.Find("BuyButton")?.GetComponent<UnityEngine.UI.Button>();
            if (hud == null || card == null || buy == null) return;

            card.sizeDelta = new Vector2(350f, 292f);
            var foodGo = card.Find("Food")?.gameObject;
            if (foodGo == null)
            {
                foodGo = new GameObject("Food", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                foodGo.transform.SetParent(card, false);
            }
            var foodRect = (RectTransform)foodGo.transform;
            foodRect.anchorMin = foodRect.anchorMax = new Vector2(.5f, 1f);
            foodRect.pivot = new Vector2(.5f, 1f);
            foodRect.anchoredPosition = new Vector2(0f, -91f);
            foodRect.sizeDelta = new Vector2(310f, 30f);
            var foodText = foodGo.GetComponent<TextMeshProUGUI>();
            foodText.alignment = TextAlignmentOptions.Center;
            foodText.fontSize = 20f;
            foodText.color = new Color(.76f, .8f, .75f);
            var loc = Object.FindObjectOfType<LocalizationManager>();
            if (loc != null && loc.Font != null) foodText.font = loc.Font;

            var buyFood = card.Find("BuyFoodButton")?.GetComponent<UnityEngine.UI.Button>();
            if (buyFood == null)
            {
                var clone = Object.Instantiate(buy.gameObject, card);
                clone.name = "BuyFoodButton";
                buyFood = clone.GetComponent<UnityEngine.UI.Button>();
            }
            var buyFoodRect = (RectTransform)buyFood.transform;
            buyFoodRect.anchoredPosition = new Vector2(0f, -170f);
            buyFoodRect.sizeDelta = new Vector2(254f, 46f);
            var localized = buyFood.GetComponentInChildren<LocalizedText>(true);
            if (localized != null) localized.SetKey(LocalizationTable.Keys.HudBuyFood);

            Set(hud, "_foodText", foodText);
            Set(hud, "_buyFoodButton", buyFood);
        }

        private static void BakeEntityPrefabs()
        {
            const string prefabRoot = "Assets/_Game/Prefabs/";
            var part = MakeEntityPrefab<MetalPart>(prefabRoot + "MetalPart.prefab", "Metal Part",
                "Assets/Resources/Art/metal_part.png", .55f, .34f);
            var contamination = MakeEntityPrefab<ContaminationSource>(prefabRoot + "Contamination.prefab", "Contamination",
                "Assets/Resources/Art/ColonyV2/contamination_oil.png", 1.35f, .5f);
            var enemy = MakeEntityPrefab<EnemyRobot>(prefabRoot + "EnemyRobot.prefab", "Scavenger Robot",
                "Assets/Resources/Art/ColonyV2/enemy_robot.png", 1.15f, .42f);
            var doctor = MakeEntityPrefab<MedicalDoctor>(prefabRoot + "MedicalDoctor.prefab", "Medical Doctor",
                "Assets/Resources/Art/ColonyV2/bug_doctor.png", 1.05f, .38f);

            var simulation = Object.FindObjectOfType<SimulationManager>();
            if (simulation != null)
            {
                Set(simulation, "_metalPartPrefab", part);
                Set(simulation, "_contaminationPrefab", contamination);
                Set(simulation, "_enemyPrefab", enemy);
                Set(simulation, "_doctorPrefab", doctor);
            }

            string creaturePath = "Assets/_Game/Prefabs/Creature.prefab";
            if (File.Exists(creaturePath))
            {
                var root = PrefabUtility.LoadPrefabContents(creaturePath);
                Component<WorldStatusBars>(root);
                var pixel = AssetDatabase.LoadAssetAtPath<Sprite>(WorldArt + "white_pixel.png");
                string[] names = { "Health Back", "Health Fill", "Age Back", "Age Fill", "Combat Back", "Combat Fill" };
                float[] ys = { .59f, .59f, .49f, .49f, .69f, .69f };
                for (int i = 0; i < names.Length; i++)
                {
                    var child = Child(root.transform, names[i]);
                    child.transform.localPosition = new Vector3(0f, ys[i], i % 2 == 0 ? 0f : -.01f);
                    child.transform.localScale = new Vector3(.62f, i % 2 == 0 ? .055f : .032f, 1f);
                    var renderer = Component<SpriteRenderer>(child);
                    renderer.sprite = pixel;
                    renderer.color = i % 2 == 0 ? new Color(.02f, .025f, .025f, .92f) : Color.white;
                    renderer.sortingOrder = i % 2 == 0 ? 40 : 41;
                    if (i >= 4) renderer.enabled = false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, creaturePath);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T MakeEntityPrefab<T>(string prefabPath, string name, string spritePath,
            float worldWidth, float colliderRadius) where T : Component
        {
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out _);
                importer.spritePixelsPerUnit = Mathf.Max(1f, sourceWidth / worldWidth);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(T));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = 8;
            go.GetComponent<CircleCollider2D>().radius = colliderRadius;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static void StyleSite(GameObject site, Sprite ring, Color color, bool squareCore)
        {
            if (site == null) return;
            site.transform.localScale = Vector3.one * .9f;
            var renderer = Component<SpriteRenderer>(site);
            renderer.sprite = ring;
            renderer.color = color;
            renderer.sortingOrder = -10;

            var core = Child(site.transform, "Site Core");
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * (squareCore ? .30f : .24f);
            var coreRenderer = Component<SpriteRenderer>(core);
            coreRenderer.sprite = ring;
            coreRenderer.color = new Color(color.r, color.g, color.b, .7f);
            coreRenderer.sortingOrder = -9;
        }

        private static void MakeLabel(Transform parent, string name, Vector3 position, string key)
        {
            var go = Child(parent, name);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * .18f;
            var text = Component<TextMeshPro>(go);
            var loc = Object.FindObjectOfType<LocalizationManager>();
            if (loc != null && loc.Font != null) text.font = loc.Font;
            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.68f, .67f, .6f, .9f);
            // Do not use outlineColor/outlineWidth here: TMP implements those by
            // reading Renderer.material, which creates leaked scene materials in edit mode.
            if (text.font != null) text.fontSharedMaterial = text.font.material;
            text.enableWordWrapping = false;
            text.sortingOrder = 20;
            var staleShadow = go.GetComponent<UnityEngine.UI.Shadow>();
            if (staleShadow != null) Object.DestroyImmediate(staleShadow);
            var localized = Component<LocalizedText>(go);
            Set(localized, "_key", key);
        }

        private static GameObject Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T Component<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void Set(Object target, string property, object value)
        {
            var serialized = new SerializedObject(target);
            var p = serialized.FindProperty(property);
            if (p == null) return;
            if (value is float f) p.floatValue = f;
            else if (value is string s) p.stringValue = s;
            else if (value is Object o) p.objectReferenceValue = o;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static Sprite EnsureSprite(string file, Texture2D texture, float ppu)
        {
            string path = WorldArt + file;
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            else Object.DestroyImmediate(texture);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D MakeGround()
        {
            const int w = 320, h = 180;
            var tex = NewTexture(w, h);
            var sand = new Color(.27f, .20f, .12f);
            var rock = new Color(.035f, .045f, .048f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                bool wall = edge < 7 + ((x * 13 + y * 7) % 5);
                float grit = ((x * 17 + y * 31) % 17 == 0) ? .016f : 0f;
                tex.SetPixel(x, y, wall ? rock : sand + new Color(grit, grit * .65f, grit * .25f));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeRing()
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            Clear(tex);
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x, y) - center;
                float dist = d.magnitude;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 180f;
                bool ring = dist > 27f && dist < 29f;
                bool ticks = dist > 24f && dist < 27f && Mathf.Repeat(angle, 45f) < 5f;
                if (ring || ticks) tex.SetPixel(x, y, Color.white);
            }
            tex.Apply(); return tex;
        }

        private static Texture2D MakePixel()
        {
            var tex = NewTexture(4, 4);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply(); return tex;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        private static void Clear(Texture2D tex)
        {
            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < tex.height; y++) for (int x = 0; x < tex.width; x++) tex.SetPixel(x, y, clear);
        }
    }
}
#endif
