#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using TCC.Core;
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
        private const string TileArt = "Assets/_Game/Art/Tiles/";

        static ScenePresentationBaker() => EditorApplication.delayCall += BakeOpenMainScene;

        [MenuItem("TCC/Play Gameplay")]
        private static void PlayGameplay()
        {
            if (Application.isPlaying) return;
            SessionState.SetBool("TCC.PlayGameplay", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("TCC/Bake Scene Presentation")]
        public static void BakeOpenMainScene()
        {
            if (Application.isPlaying) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != "Assets/_Game/Scenes/Main.unity") return;

            NormalizePixelArtImporters();
            BakeWorld();
            BakeFacilityPrefabsAndPlacement();
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

        private static void NormalizePixelArtImporters()
        {
            foreach (string root in new[] { "Assets/Resources/Art", "Assets/_Game/Art" })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    bool dirty = importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                        importer.textureCompression != TextureImporterCompression.Uncompressed;
                    if (!dirty) continue;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void BakeWorld()
        {
            Directory.CreateDirectory(WorldArt);
            var pit = LoadWorldSprite(WorldArt + "nursery_pit.png", 3.8f);
            var grid = EnsureSprite("placement_grid.png", MakePlacementGrid(), 20f);

            var world = GameObject.Find("[World]")?.transform;
            if (world == null) return;

            var backdrop = world.Find("Cave Backdrop");
            if (backdrop != null) Object.DestroyImmediate(backdrop.gameObject);
            BakeTerrainTilemap(world);

            if (Camera.main != null)
            {
                var pixelCamera = Component<PixelPerfectCamera>(Camera.main.gameObject);
                // Full HD is an exact 3x presentation of this reference canvas. This
                // preserves cracks, rivets and anatomy without blurred resampling.
                pixelCamera.assetsPPU = 32;
                pixelCamera.refResolutionX = 640;
                pixelCamera.refResolutionY = 360;
                pixelCamera.upscaleRT = true;
                pixelCamera.pixelSnapping = false;
                pixelCamera.cropFrameX = false;
                pixelCamera.cropFrameY = false;
                pixelCamera.stretchFill = false;
            }

            var birth = GameObject.Find("BirthCircle");
            if (birth != null)
            {
                birth.transform.localScale = Vector3.one;
                var birthRenderer = Component<SpriteRenderer>(birth);
                birthRenderer.sprite = pit;
                birthRenderer.color = Color.white;
                birthRenderer.sortingOrder = -12;
                var core = birth.transform.Find("Site Core");
                if (core != null) Object.DestroyImmediate(core.gameObject);
            }
            MakeLabel(world, "Nursery Label", new Vector3(-6.8f, -2.05f, 0f),
                LocalizationTable.Keys.ZoneNursery);

            var gridGo = Child(world, "Placement Grid Overlay");
            gridGo.transform.localPosition = new Vector3(-3f, 0f, 0f);
            gridGo.transform.localScale = Vector3.one;
            var gridRenderer = Component<SpriteRenderer>(gridGo);
            gridRenderer.sprite = grid;
            gridRenderer.color = new Color(.32f, .8f, .67f, .36f);
            gridRenderer.sortingOrder = -40;
            gridGo.SetActive(false);

            var oldRiver = GameObject.Find("Sand River");
            if (oldRiver != null) Object.DestroyImmediate(oldRiver);
            var oldPit = GameObject.Find("Pit");
            if (oldPit != null) Object.DestroyImmediate(oldPit);
            var oldBarracks = GameObject.Find("BarracksSite");
            if (oldBarracks != null) Object.DestroyImmediate(oldBarracks);
        }

        private static void BakeTerrainTilemap(Transform world)
        {
            Directory.CreateDirectory(TileArt);
            var sandSprites = new Sprite[4];
            for (int i = 0; i < sandSprites.Length; i++)
            {
                var sprite = EnsureSpriteAt(TileArt + $"sand_{i}.png", MakeTerrainTile(i, false), 64f);
                sandSprites[i] = sprite;
            }
            var groundRule = EnsureGroundRuleTile(TileArt + "desert_ground_rule.asset", sandSprites);
            var rock = new Tile[2];
            for (int i = 0; i < rock.Length; i++)
            {
                var sprite = EnsureSpriteAt(TileArt + $"rock_edge_{i}.png", MakeTerrainTile(i, true), 64f);
                rock[i] = EnsureTile(TileArt + $"rock_edge_{i}.asset", sprite, $"Rock Edge {i}");
            }

            var gridGo = Child(world, "Terrain Grid");
            gridGo.transform.localPosition = new Vector3(-10f, -5.75f, 0f);
            gridGo.transform.localScale = Vector3.one;
            var terrainGrid = Component<Grid>(gridGo);
            terrainGrid.cellSize = new Vector3(.5f, .5f, 0f);
            terrainGrid.cellGap = Vector3.zero;

            var groundGo = Child(gridGo.transform, "Ground Tilemap");
            var ground = Component<Tilemap>(groundGo);
            var groundRenderer = Component<TilemapRenderer>(groundGo);
            groundRenderer.sortingOrder = -100;
            ground.ClearAllTiles();

            var borderGo = Child(gridGo.transform, "Cave Edge Tilemap");
            var border = Component<Tilemap>(borderGo);
            var borderRenderer = Component<TilemapRenderer>(borderGo);
            borderRenderer.sortingOrder = -99;
            border.ClearAllTiles();

            const int width = 40, height = 23;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int hash = Mathf.Abs(x * 17 + y * 31 + x * y * 3);
                ground.SetTile(new Vector3Int(x, y, 0), groundRule);
                bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                if (edge) border.SetTile(new Vector3Int(x, y, 0), rock[hash % rock.Length]);
            }
            ground.CompressBounds();
            border.CompressBounds();
        }

        private static RuleTile EnsureGroundRuleTile(string path, Sprite[] sprites)
        {
            var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.m_DefaultSprite = sprites[0];
            tile.m_DefaultColliderType = Tile.ColliderType.None;
            tile.m_TilingRules.Clear();
            var rule = new RuleTile.TilingRule
            {
                m_Sprites = sprites,
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_PerlinScale = .31f,
                m_ColliderType = Tile.ColliderType.None
            };
            rule.m_Neighbors.Clear();
            rule.m_NeighborPositions.Clear();
            tile.m_TilingRules.Add(rule);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tile EnsureTile(string path, Sprite sprite, string name)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.name = Path.GetFileNameWithoutExtension(path);
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void BakeFacilityPrefabsAndPlacement()
        {
            Directory.CreateDirectory("Assets/_Game/Prefabs");
            var ring = EnsureSprite("site_ring.png", MakeRing(), 32f);
            var pixel = EnsureSprite("white_pixel.png", MakePixel(), 4f);
            var level2Decor = EnsureSprite("facility_level2_decor.png", MakeFacilityDecor(false), 32f);
            var level3Decor = EnsureSprite("facility_level3_decor.png", MakeFacilityDecor(true), 32f);
            var factory = MakeFacilityPrefab(FacilityType.Factory, "Factory Facility",
                "Assets/Resources/Art/Facilities/facility_factory.png",
                "Assets/_Game/Prefabs/FactoryFacility.prefab", LocalizationTable.Keys.ZoneFactory, ring, pixel, level2Decor, level3Decor);
            var barracks = MakeFacilityPrefab(FacilityType.Barracks, "Barracks Facility",
                "Assets/Resources/Art/Facilities/facility_barracks.png",
                "Assets/_Game/Prefabs/BarracksFacility.prefab", LocalizationTable.Keys.ZoneBarracks, ring, pixel, level2Decor, level3Decor);
            var hospital = MakeFacilityPrefab(FacilityType.Hospital, "Hospital Facility",
                "Assets/Resources/Art/Facilities/facility_hospital.png",
                "Assets/_Game/Prefabs/HospitalFacility.prefab", LocalizationTable.Keys.ZoneHospital, ring, pixel, level2Decor, level3Decor);
            var academy = MakeFacilityPrefab(FacilityType.Academy, "Academy Facility",
                "Assets/Resources/Art/Facilities/facility_academy.png",
                "Assets/_Game/Prefabs/AcademyFacility.prefab", LocalizationTable.Keys.ZoneAcademy, ring, pixel, level2Decor, level3Decor);

            var world = GameObject.Find("[World]")?.transform;
            if (world == null) return;
            foreach (var facility in Object.FindObjectsOfType<ColonyFacility>(true))
                if (facility != null && facility.gameObject.scene.IsValid()) Object.DestroyImmediate(facility.gameObject);
            foreach (string stale in new[] { "LaborCircle", "Factory Site", "Factory Label", "Barracks Label", "Hospital Label", "Academy Label" })
            {
                var go = GameObject.Find(stale);
                if (go != null) Object.DestroyImmediate(go);
            }

            var managers = GameObject.Find("[Managers]")?.transform;
            if (managers == null) return;
            var placementGo = Child(managers, "Building Placement Manager");
            var placement = Component<BuildingPlacementManager>(placementGo);
            Set(placement, "_factoryPrefab", factory);
            Set(placement, "_barracksPrefab", barracks);
            Set(placement, "_hospitalPrefab", hospital);
            Set(placement, "_academyPrefab", academy);
            Set(placement, "_worldRoot", world);
            Set(placement, "_gridOverlay", world.Find("Placement Grid Overlay")?.gameObject);
        }

        private static ColonyFacility MakeFacilityPrefab(FacilityType type, string name, string spritePath,
            string prefabPath, string labelKey, Sprite halo, Sprite pixel, Sprite level2Sprite, Sprite level3Sprite)
        {
            var structureSprite = LoadWorldSprite(spritePath, 3.8f);
            var root = new GameObject(name, typeof(CircleCollider2D), typeof(ColonyFacility));

            var footprint = Child(root.transform, "Reserved Maximum Footprint");
            var footprintRenderer = Component<SpriteRenderer>(footprint);
            footprintRenderer.sprite = pixel;
            footprintRenderer.sortingOrder = -18;
            footprintRenderer.enabled = false;

            var upgradeHalo = Child(root.transform, "Upgrade Halo");
            var haloRenderer = Component<SpriteRenderer>(upgradeHalo);
            haloRenderer.sprite = halo;
            haloRenderer.sortingOrder = 2;

            var structure = Child(root.transform, "Hollow Perimeter Structure");
            var structureRenderer = Component<SpriteRenderer>(structure);
            structureRenderer.sprite = structureSprite;
            structureRenderer.sortingOrder = 3;

            var level2Go = Child(root.transform, "Level 2 Utility Modules");
            var level2Renderer = Component<SpriteRenderer>(level2Go);
            level2Renderer.sprite = level2Sprite;
            level2Renderer.sortingOrder = 4;
            level2Renderer.enabled = false;
            var level3Go = Child(root.transform, "Level 3 Command Modules");
            var level3Renderer = Component<SpriteRenderer>(level3Go);
            level3Renderer.sprite = level3Sprite;
            level3Renderer.sortingOrder = 5;
            level3Renderer.enabled = false;

            var back = Child(root.transform, "Facility Progress Back");
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

            MakeLabel(root.transform, "Facility Label", Vector3.zero, labelKey);
            var label = root.transform.Find("Facility Label")?.GetComponent<TMP_Text>();
            var facility = root.GetComponent<ColonyFacility>();
            facility.Configure(type, 1.15f, structureRenderer, haloRenderer, backRenderer, fillRenderer);
            facility.ConfigurePresentation(footprintRenderer, label, level2Renderer, level3Renderer, 2.2f, 3.8f);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab != null ? prefab.GetComponent<ColonyFacility>() : null;
        }

        private static void BakeHudExtensions()
        {
            var hud = Object.FindObjectOfType<HudView>(true);
            var card = GameObject.Find("HudCard")?.transform as RectTransform;
            var buy = GameObject.Find("BuyButton")?.GetComponent<UnityEngine.UI.Button>();
            if (hud == null || card == null || buy == null) return;

            var sidebar = BakeRightSidebar(card.parent, card);
            card.sizeDelta = new Vector2(386f, 286f);
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
            BakeInventory(sidebar);
            BakeBuildPalette(sidebar);
            BakeTitlePresentation();
        }

        private static Transform BakeRightSidebar(Transform canvas, RectTransform hudCard)
        {
            var sidebar = UIChild(canvas, "Right Command Sidebar");
            var rect = sidebar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(430f, 0f);
            var image = Component<UnityEngine.UI.Image>(sidebar);
            image.color = new Color(.008f, .014f, .017f, 1f);
            PixelChrome.Apply(sidebar, new Color(.12f, .44f, .44f, .95f), new Color(.52f, .36f, .18f, .9f));
            sidebar.transform.SetAsLastSibling();

            foreach (string panelName in new[] { "Base Construction Palette", "Cargo Backpack" })
            {
                var existing = canvas.Find(panelName);
                if (existing != null) existing.SetParent(rect, false);
            }

            if (hudCard != null)
            {
                hudCard.SetParent(rect, false);
                hudCard.anchorMin = hudCard.anchorMax = new Vector2(.5f, 0f);
                hudCard.pivot = new Vector2(.5f, 0f);
                hudCard.anchoredPosition = new Vector2(0f, 18f);
            }
            return rect;
        }

        private static void BakeTitlePresentation()
        {
            var canvas = GameObject.Find("TitleMenuCanvas");
            if (canvas == null) return;
            var canvasRect = canvas.transform as RectTransform;
            var artwork = UIChild(canvasRect, "Title Artwork");
            var artworkRect = artwork.GetComponent<RectTransform>();
            artworkRect.anchorMin = Vector2.zero; artworkRect.anchorMax = Vector2.one;
            artworkRect.offsetMin = artworkRect.offsetMax = Vector2.zero;
            artwork.transform.SetAsFirstSibling();

            var vista = UIChild(artworkRect, "Cave Vista");
            var vistaRect = vista.GetComponent<RectTransform>();
            vistaRect.anchorMin = Vector2.zero; vistaRect.anchorMax = Vector2.one;
            vistaRect.offsetMin = vistaRect.offsetMax = Vector2.zero;
            var vistaImage = Component<UnityEngine.UI.Image>(vista);
            vistaImage.sprite = LoadUiSprite("Assets/_Game/Art/Title/title_cave_vista.png");
            vistaImage.color = Color.white;
            vistaImage.preserveAspect = false;
            vistaImage.raycastTarget = false;

            var grade = UIChild(artworkRect, "Cinematic Grade");
            var gradeRect = grade.GetComponent<RectTransform>();
            gradeRect.anchorMin = Vector2.zero; gradeRect.anchorMax = Vector2.one;
            gradeRect.offsetMin = gradeRect.offsetMax = Vector2.zero;
            var gradeImage = Component<UnityEngine.UI.Image>(grade);
            gradeImage.color = new Color(.005f, .012f, .015f, .18f);
            gradeImage.raycastTarget = false;

            var logo = UIChild(artworkRect, "English Art Logo");
            var logoRect = logo.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(.5f, 1f);
            logoRect.pivot = new Vector2(.5f, 1f);
            logoRect.anchoredPosition = new Vector2(0f, -20f);
            logoRect.sizeDelta = new Vector2(1040f, 500f);
            var logoImage = Component<UnityEngine.UI.Image>(logo);
            logoImage.sprite = LoadUiSprite("Assets/_Game/Art/Title/title_logo_en.png");
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            var dim = canvas.transform.Find("Dim") as RectTransform;
            if (dim != null)
            {
                dim.SetSiblingIndex(1);
                var dimImage = dim.GetComponent<UnityEngine.UI.Image>();
                if (dimImage != null) dimImage.color = new Color(.008f, .014f, .016f, .18f);
            }

            var box = canvas.transform.Find("Box") as RectTransform;
            if (box == null) return;
            box.SetAsLastSibling();
            box.anchorMin = box.anchorMax = new Vector2(.5f, .5f);
            box.pivot = new Vector2(.5f, .5f);
            box.anchoredPosition = new Vector2(0f, -245f);
            box.sizeDelta = new Vector2(470f, 390f);
            var boxImage = box.GetComponent<UnityEngine.UI.Image>();
            if (boxImage != null) boxImage.color = new Color(.012f, .024f, .027f, .9f);
            PixelChrome.Apply(box.gameObject, new Color(.2f, .62f, .58f, .95f), new Color(.72f, .48f, .2f, 1f));

            var subtitle = box.Find("Title") as RectTransform ?? box.Find("Localized Subtitle") as RectTransform;
            if (subtitle != null)
            {
                subtitle.name = "Localized Subtitle";
                subtitle.anchorMin = subtitle.anchorMax = new Vector2(.5f, .5f);
                subtitle.pivot = new Vector2(.5f, .5f);
                subtitle.anchoredPosition = new Vector2(0f, 145f);
                subtitle.sizeDelta = new Vector2(420f, 52f);
                var text = subtitle.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.fontSize = 30f;
                    text.color = new Color(.88f, .72f, .42f, 1f);
                    text.alignment = TextAlignmentOptions.Center;
                }
            }

            SetMenuRect(box.Find("StartButton") as RectTransform, 70f);
            SetMenuRect(box.Find("ContinueButton") as RectTransform, 4f);
            SetMenuRect(box.Find("SettingsButton") as RectTransform, -62f);
            SetMenuRect(box.Find("QuitButton") as RectTransform, -128f);

            var view = canvas.GetComponent<TitleMenuView>();
            if (view != null)
            {
                Set(view, "_localizedSubtitle", subtitle != null ? subtitle.gameObject : null);
                Set(view, "_titleArtwork", artwork);
            }
        }

        private static void SetMenuRect(RectTransform rect, float y)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(370f, 54f);
            var text = rect.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.fontSize = 22f;
        }

        private static void BakeBuildPalette(Transform canvas)
        {
            if (canvas == null) return;
            var panel = UIChild(canvas, "Base Construction Palette");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(386f, 350f);
            var image = Component<UnityEngine.UI.Image>(panel);
            image.color = new Color(.014f, .026f, .03f, .96f);
            PixelChrome.Apply(panel, new Color(.18f, .64f, .62f, 1f), new Color(.78f, .52f, .22f, 1f));

            foreach (string stale in new[] { "Build Title", "Factory Build Card", "Barracks Build Card", "Hospital Build Card", "Academy Build Card" })
            {
                var child = rect.Find(stale);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }

            var titleGo = UIChild(rect, "Command Title");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            titleRect.sizeDelta = new Vector2(-24f, 25f);
            var title = Component<TextMeshProUGUI>(titleGo);
            ApplyUiText(title, 16f, new Color(.84f, .7f, .4f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(titleGo).SetKey(LocalizationTable.Keys.HudCommand);

            var buildTab = TabButton(rect, "Build Tab", LocalizationTable.Keys.HudTabBuild, -124f);
            var researchTab = TabButton(rect, "Research Tab", LocalizationTable.Keys.HudTabResearch, 0f);
            var equipmentTab = TabButton(rect, "Equipment Tab", LocalizationTable.Keys.HudTabEquipment, 124f);

            var buildPage = UIChild(rect, "Build Page");
            var buildRect = buildPage.GetComponent<RectTransform>();
            buildRect.anchorMin = Vector2.zero; buildRect.anchorMax = Vector2.one;
            buildRect.offsetMin = new Vector2(10f, 8f);
            buildRect.offsetMax = new Vector2(-10f, -74f);

            var hintGo = UIChild(buildRect, "Build Hint");
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 1f); hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -1f);
            hintRect.sizeDelta = new Vector2(-12f, 22f);
            var hint = Component<TextMeshProUGUI>(hintGo);
            ApplyUiText(hint, 12f, new Color(.58f, .68f, .62f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(hintGo).SetKey(LocalizationTable.Keys.HudBuild);

            BuildCard(buildRect, "Factory Build Card", FacilityType.Factory, LocalizationTable.Keys.BuildFactory,
                "Assets/Resources/Art/Facilities/facility_factory.png", -91f, -28f);
            BuildCard(buildRect, "Barracks Build Card", FacilityType.Barracks, LocalizationTable.Keys.BuildBarracks,
                "Assets/Resources/Art/Facilities/facility_barracks.png", 91f, -28f);
            BuildCard(buildRect, "Hospital Build Card", FacilityType.Hospital, LocalizationTable.Keys.BuildHospital,
                "Assets/Resources/Art/Facilities/facility_hospital.png", -91f, -150f);
            BuildCard(buildRect, "Academy Build Card", FacilityType.Academy, LocalizationTable.Keys.BuildAcademy,
                "Assets/Resources/Art/Facilities/facility_academy.png", 91f, -150f);

            var researchPage = UIChild(rect, "Research Page");
            var researchRect = researchPage.GetComponent<RectTransform>();
            researchRect.anchorMin = Vector2.zero; researchRect.anchorMax = Vector2.one;
            researchRect.offsetMin = new Vector2(12f, 12f);
            researchRect.offsetMax = new Vector2(-12f, -80f);
            var researchHintGo = UIChild(researchRect, "Research Hint");
            var researchHintRect = researchHintGo.GetComponent<RectTransform>();
            researchHintRect.anchorMin = new Vector2(0f, 1f); researchHintRect.anchorMax = new Vector2(1f, 1f);
            researchHintRect.pivot = new Vector2(.5f, 1f);
            researchHintRect.anchoredPosition = Vector2.zero;
            researchHintRect.sizeDelta = new Vector2(0f, 28f);
            var researchHint = Component<TextMeshProUGUI>(researchHintGo);
            ApplyUiText(researchHint, 12f, new Color(.66f, .72f, .66f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(researchHintGo).SetKey(LocalizationTable.Keys.HudResearchReserved);
            ResearchSlot(researchRect, "Medical Doctor Talent", LocalizationTable.Keys.HudResearchDoctor,
                "Assets/Resources/Art/ColonyV2/bug_doctor.png", -91f, -36f, true);
            ResearchSlot(researchRect, "Future Talent 1", LocalizationTable.Keys.HudResearchFuture,
                null, 91f, -36f, false);
            ResearchSlot(researchRect, "Future Talent 2", LocalizationTable.Keys.HudResearchFuture,
                null, -91f, -154f, false);
            ResearchSlot(researchRect, "Future Talent 3", LocalizationTable.Keys.HudResearchFuture,
                null, 91f, -154f, false);
            researchPage.SetActive(false);

            var equipmentPage = UIChild(rect, "Equipment Page");
            var equipmentRect = equipmentPage.GetComponent<RectTransform>();
            equipmentRect.anchorMin = Vector2.zero; equipmentRect.anchorMax = Vector2.one;
            equipmentRect.offsetMin = new Vector2(18f, 14f);
            equipmentRect.offsetMax = new Vector2(-18f, -80f);
            var equipmentHintGo = UIChild(equipmentRect, "Equipment Hint");
            var equipmentHintRect = equipmentHintGo.GetComponent<RectTransform>();
            equipmentHintRect.anchorMin = new Vector2(0f, 1f); equipmentHintRect.anchorMax = new Vector2(1f, 1f);
            equipmentHintRect.pivot = new Vector2(.5f, 1f);
            equipmentHintRect.anchoredPosition = Vector2.zero;
            equipmentHintRect.sizeDelta = new Vector2(0f, 28f);
            var equipmentHint = Component<TextMeshProUGUI>(equipmentHintGo);
            ApplyUiText(equipmentHint, 13f, new Color(.66f, .72f, .66f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(equipmentHintGo).SetKey(LocalizationTable.Keys.HudEquipmentReserved);
            EquipmentSlot(equipmentRect, "Armor Module Slot", LocalizationTable.Keys.HudEquipmentArmor, -112f);
            EquipmentSlot(equipmentRect, "Weapon Module Slot", LocalizationTable.Keys.HudEquipmentTool, 0f);
            EquipmentSlot(equipmentRect, "Core Module Slot", LocalizationTable.Keys.HudEquipmentCore, 112f);
            equipmentPage.SetActive(false);

            Component<CommandDockView>(panel).Configure(buildPage, researchPage, equipmentPage,
                buildTab.button, researchTab.button, equipmentTab.button,
                buildTab.image, researchTab.image, equipmentTab.image);
        }

        private static (UnityEngine.UI.Button button, UnityEngine.UI.Image image) TabButton(
            RectTransform parent, string name, string key, float x)
        {
            var go = UIChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -38f);
            rect.sizeDelta = new Vector2(116f, 30f);
            var image = Component<UnityEngine.UI.Image>(go);
            image.color = new Color(.025f, .045f, .05f, .95f);
            PixelChrome.Apply(go, new Color(.15f, .48f, .48f, 1f), new Color(.65f, .45f, .2f, 1f));
            var button = Component<UnityEngine.UI.Button>(go);
            button.targetGraphic = image;
            var labelGo = UIChild(rect, "Label");
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = Component<TextMeshProUGUI>(labelGo);
            ApplyUiText(label, 14f, new Color(.8f, .78f, .64f, 1f), TextAlignmentOptions.Center);
            label.raycastTarget = false;
            Component<LocalizedText>(labelGo).SetKey(key);
            return (button, image);
        }

        private static void EquipmentSlot(RectTransform parent, string name, string key, float x)
        {
            var go = UIChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(x, 2f);
            rect.sizeDelta = new Vector2(112f, 116f);
            var image = Component<UnityEngine.UI.Image>(go);
            image.color = new Color(.025f, .04f, .045f, .94f);
            PixelChrome.Apply(go, new Color(.16f, .34f, .34f, .9f), new Color(.46f, .34f, .18f, .9f));
            var socket = UIChild(rect, "Socket");
            var socketRect = socket.GetComponent<RectTransform>();
            socketRect.anchorMin = socketRect.anchorMax = new Vector2(.5f, 1f);
            socketRect.pivot = new Vector2(.5f, 1f);
            socketRect.anchoredPosition = new Vector2(0f, -10f);
            socketRect.sizeDelta = new Vector2(72f, 72f);
            var socketImage = Component<UnityEngine.UI.Image>(socket);
            socketImage.sprite = LoadUiSprite("Assets/Resources/Art/Inventory/elite_equipment.png");
            socketImage.color = new Color(.35f, .42f, .4f, .22f);
            socketImage.preserveAspect = true;
            var labelGo = UIChild(rect, "Label");
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            labelRect.sizeDelta = new Vector2(-8f, 25f);
            var label = Component<TextMeshProUGUI>(labelGo);
            ApplyUiText(label, 13f, new Color(.62f, .65f, .58f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(labelGo).SetKey(key);
        }

        private static void ResearchSlot(RectTransform parent, string name, string key,
            string iconPath, float x, float y, bool available)
        {
            var go = UIChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(170f, 106f);
            var image = Component<UnityEngine.UI.Image>(go);
            image.color = available ? new Color(.035f, .09f, .085f, .96f)
                : new Color(.02f, .03f, .034f, .9f);
            PixelChrome.Apply(go, available ? new Color(.18f, .52f, .48f, .9f)
                : new Color(.12f, .22f, .22f, .75f), new Color(.48f, .34f, .18f, .85f));

            var socket = UIChild(rect, "Portrait");
            var socketRect = socket.GetComponent<RectTransform>();
            socketRect.anchorMin = socketRect.anchorMax = new Vector2(.5f, 1f);
            socketRect.pivot = new Vector2(.5f, 1f);
            socketRect.anchoredPosition = new Vector2(0f, -6f);
            socketRect.sizeDelta = new Vector2(70f, 66f);
            var socketImage = Component<UnityEngine.UI.Image>(socket);
            socketImage.sprite = string.IsNullOrEmpty(iconPath) ? null : LoadUiSprite(iconPath);
            socketImage.enabled = socketImage.sprite != null;
            socketImage.color = available ? Color.white : new Color(.25f, .3f, .3f, .25f);
            socketImage.preserveAspect = true;
            socketImage.raycastTarget = false;

            var labelGo = UIChild(rect, "Label");
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            labelRect.sizeDelta = new Vector2(-8f, 25f);
            var label = Component<TextMeshProUGUI>(labelGo);
            ApplyUiText(label, 12f, available ? new Color(.76f, .8f, .68f, 1f)
                : new Color(.38f, .44f, .42f, 1f), TextAlignmentOptions.Center);
            Component<LocalizedText>(labelGo).SetKey(key);
        }

        private static void BuildCard(RectTransform parent, string name, FacilityType type,
            string labelKey, string iconPath, float x, float y)
        {
            var card = UIChild(parent, name);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(170f, 112f);
            var background = Component<UnityEngine.UI.Image>(card);
            background.color = new Color(.045f, .12f, .12f, .98f);
            PixelChrome.Apply(card, new Color(.2f, .58f, .57f, .9f), new Color(.64f, .45f, .22f, .9f));

            var iconGo = UIChild(rect, "Facility Icon");
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, 1f);
            iconRect.pivot = new Vector2(.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -7f);
            iconRect.sizeDelta = new Vector2(72f, 66f);
            var icon = Component<UnityEngine.UI.Image>(iconGo);
            icon.sprite = LoadUiSprite(iconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelGo = UIChild(rect, "Build Label");
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 6f);
            labelRect.sizeDelta = new Vector2(-10f, 38f);
            var label = Component<TextMeshProUGUI>(labelGo);
            ApplyUiText(label, 12f, new Color(.82f, .8f, .68f, 1f), TextAlignmentOptions.Center);
            label.enableWordWrapping = true;
            label.raycastTarget = false;

            Component<BuildCardView>(card).Configure(type, labelKey, label, background, icon);
        }

        private static void ApplyUiText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
        {
            var loc = Object.FindObjectOfType<LocalizationManager>();
            if (loc != null && loc.Font != null) text.font = loc.Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
        }

        private static void BakeInventory(Transform canvas)
        {
            if (canvas == null) return;
            var economy = Object.FindObjectOfType<EconomyManager>(true);
            if (economy != null) Component<InventoryManager>(economy.gameObject);

            var panel = UIChild(canvas, "Cargo Backpack");
            var panelRect = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, 1f);
            panelRect.pivot = new Vector2(.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -382f);
            panelRect.sizeDelta = new Vector2(386f, 360f);
            var panelImage = Component<UnityEngine.UI.Image>(panel);
            panelImage.color = new Color(.018f, .028f, .032f, .96f);
            PixelChrome.Apply(panel, new Color(.22f, .72f, .68f, 1f), new Color(.82f, .58f, .24f, 1f));

            var titleGo = UIChild(panelRect, "Backpack Title");
            var titleRect = titleGo.GetComponent<RectTransform>() ?? titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);
            titleRect.sizeDelta = new Vector2(-28f, 32f);
            var title = Component<TextMeshProUGUI>(titleGo);
            title.fontSize = 20f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(.82f, .78f, .62f, 1f);
            var loc = Object.FindObjectOfType<LocalizationManager>();
            if (loc != null && loc.Font != null) title.font = loc.Font;
            Component<LocalizedText>(titleGo).SetKey(LocalizationTable.Keys.HudInventory);

            var viewport = UIChild(panelRect, "Viewport");
            var viewportRect = viewport.GetComponent<RectTransform>() ?? viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(18f, 16f);
            viewportRect.offsetMax = new Vector2(-18f, -48f);
            var viewportImage = Component<UnityEngine.UI.Image>(viewport);
            viewportImage.color = new Color(.01f, .016f, .018f, .82f);
            Component<UnityEngine.UI.RectMask2D>(viewport);

            var content = UIChild(viewportRect, "Grid Content");
            var contentRect = content.GetComponent<RectTransform>() ?? content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 298f);
            var grid = Component<UnityEngine.UI.GridLayoutGroup>(content);
            grid.cellSize = new Vector2(106f, 68f);
            grid.spacing = new Vector2(7f, 5f);
            grid.padding = new RectOffset(7, 7, 7, 7);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;

            const int slotCount = 12;
            for (int i = contentRect.childCount - 1; i >= 0; i--)
            {
                var child = contentRect.GetChild(i);
                if (!child.name.StartsWith("Cargo Slot ")) continue;
                if (int.TryParse(child.name.Substring("Cargo Slot ".Length), out int number) && number > slotCount)
                    Object.DestroyImmediate(child.gameObject);
            }
            var slots = new InventorySlotView[slotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                var slotGo = UIChild(contentRect, $"Cargo Slot {i + 1:00}");
                var slotRect = slotGo.GetComponent<RectTransform>() ?? slotGo.AddComponent<RectTransform>();
                slotRect.sizeDelta = grid.cellSize;
                var background = Component<UnityEngine.UI.Image>(slotGo);
                background.color = new Color(.025f, .035f, .04f, .78f);
                PixelChrome.Apply(slotGo, new Color(.18f, .34f, .34f, .82f), new Color(.52f, .39f, .2f, .9f));

                var iconGo = UIChild(slotRect, "Icon");
                var iconRect = iconGo.GetComponent<RectTransform>() ?? iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(.5f, .5f); iconRect.anchorMax = new Vector2(.5f, .5f);
                iconRect.pivot = new Vector2(.5f, .5f);
                iconRect.sizeDelta = new Vector2(52f, 52f);
                iconRect.anchoredPosition = new Vector2(0f, 2f);
                var icon = Component<UnityEngine.UI.Image>(iconGo);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;
                icon.color = Color.white;

                var countGo = UIChild(slotRect, "Count");
                var countRect = countGo.GetComponent<RectTransform>() ?? countGo.AddComponent<RectTransform>();
                countRect.anchorMin = new Vector2(1f, 0f); countRect.anchorMax = new Vector2(1f, 0f);
                countRect.pivot = new Vector2(1f, 0f);
                countRect.anchoredPosition = new Vector2(-6f, 4f);
                countRect.sizeDelta = new Vector2(44f, 25f);
                var count = Component<TextMeshProUGUI>(countGo);
                if (loc != null && loc.Font != null) count.font = loc.Font;
                count.fontSize = 18f;
                count.alignment = TextAlignmentOptions.BottomRight;
                count.color = new Color(.9f, .82f, .58f, 1f);
                count.raycastTarget = false;

                slots[i] = Component<InventorySlotView>(slotGo);
                slots[i].Configure(icon, count, background);
            }

            var scroll = Component<UnityEngine.UI.ScrollRect>(panel);
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            string root = "Assets/Resources/Art/Inventory/";
            var icons = new[]
            {
                LoadUiSprite(root + "food_ration.png"),
                LoadUiSprite(root + "metal_scrap.png"),
                LoadUiSprite(root + "refined_component.png"),
                LoadUiSprite(root + "elite_equipment.png")
            };
            var view = Component<InventoryView>(panel);
            view.Configure(slots, icons);
        }

        private static void BakeEntityPrefabs()
        {
            const string prefabRoot = "Assets/_Game/Prefabs/";
            var part = MakeEntityPrefab<MetalPart>(prefabRoot + "MetalPart.prefab", "Metal Part",
                "Assets/Resources/Art/Inventory/metal_scrap.png", .55f, .34f);
            var contamination = MakeEntityPrefab<ContaminationSource>(prefabRoot + "Contamination.prefab", "Contamination",
                "Assets/Resources/Art/ColonyV2/contamination_oil.png", 1.35f, .5f);
            var enemy = MakeEntityPrefab<EnemyRobot>(prefabRoot + "EnemyRobot.prefab", "Scavenger Robot",
                "Assets/Resources/Art/ColonyV2/enemy_robot.png", 1.15f, .42f);
            var heavyEnemy = MakeEntityPrefab<EnemyRobot>(prefabRoot + "HeavyEnemyRobot.prefab", "Heavy Invader",
                "Assets/Resources/Art/Enemies/enemy_heavy.png", 3.45f, .95f);
            Set(heavyEnemy, "_heavy", true);
            var doctor = MakeEntityPrefab<MedicalDoctor>(prefabRoot + "MedicalDoctor.prefab", "Medical Doctor",
                "Assets/Resources/Art/ColonyV2/bug_doctor.png", 1.05f, .38f);

            var simulation = Object.FindObjectOfType<SimulationManager>();
            if (simulation != null)
            {
                Set(simulation, "_metalPartPrefab", part);
                Set(simulation, "_contaminationPrefab", contamination);
                Set(simulation, "_enemyPrefab", enemy);
                Set(simulation, "_heavyEnemyPrefab", heavyEnemy);
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
            go.transform.localScale = Vector3.one * .09f;
            var text = Component<TextMeshPro>(go);
            var loc = Object.FindObjectOfType<LocalizationManager>();
            if (loc != null && loc.Font != null) text.font = loc.Font;
            text.fontSize = 32f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.68f, .67f, .6f, .9f);
            // Do not use outlineColor/outlineWidth here: TMP implements those by
            // reading Renderer.material, which creates leaked scene materials in edit mode.
            if (text.font != null) text.fontSharedMaterial = text.font.material;
            text.enableWordWrapping = false;
            text.rectTransform.sizeDelta = new Vector2(56f, 7f);
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

        private static GameObject UIChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
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
            else if (value is bool b) p.boolValue = b;
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

        private static Sprite EnsureSpriteAt(string path, Texture2D texture, float ppu)
        {
            byte[] bytes = texture.EncodeToPNG();
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            Object.DestroyImmediate(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadUiSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 96f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadWorldSprite(string path, float worldWidth)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.GetSourceTextureWidthAndHeight(out int width, out _);
                importer.spritePixelsPerUnit = Mathf.Max(1f, width / worldWidth);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
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

        private static Texture2D MakeTerrainTile(int variant, bool rock)
        {
            const int size = 32;
            var tex = NewTexture(size, size);
            Color baseColor = rock ? new Color(.075f, .082f, .078f, 1f)
                : new Color(.265f, .195f, .118f, 1f);
            Color mid = rock ? new Color(.105f, .112f, .103f, 1f)
                : new Color(.305f, .222f, .128f, 1f);
            Color light = rock ? new Color(.155f, .155f, .135f, 1f)
                : new Color(.365f, .260f, .145f, 1f);
            Color dark = rock ? new Color(.032f, .040f, .041f, 1f)
                : new Color(.185f, .128f, .075f, 1f);
            Color deep = rock ? new Color(.014f, .020f, .023f, 1f)
                : new Color(.125f, .082f, .050f, 1f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                // Coherent patches read as packed earth/stone. Single-pixel confetti
                // was the main source of the previous abstract appearance.
                int field = Mathf.Abs((x / 4) * 19 + (y / 3) * 31 + variant * 43);
                tex.SetPixel(x, y, field % 7 == 0 ? mid : baseColor);
            }

            if (rock)
            {
                // Mortared cave blocks with chipped corners and recessed seams.
                int seamX = 10 + variant * 3;
                PaintRect(tex, 1, 8, 29, 1, deep);
                PaintRect(tex, 1, 9, 29, 1, light);
                PaintRect(tex, seamX, 1, 1, 7, deep);
                PaintRect(tex, (seamX + 11) % 27 + 2, 10, 1, 20, deep);
                PaintRect(tex, 3 + variant * 2, 4, 4, 2, mid);
                PaintRect(tex, 24 - variant, 22, 3, 2, mid);
                DrawCrack(tex, 17 + variant, 15, variant % 2 == 0 ? 1 : -1, dark, light);
            }
            else
            {
                // Worn ochre flagstones echo the dense dungeon-floor language of the
                // art reference while remaining desert material. Two-pixel seams
                // survive the 2:1 source-to-reference reduction as deliberate pixels.
                // A restrained flagstone grid communicates buildable cells without
                // becoming the dominant texture. Functional placement uses a brighter
                // overlay only while the player is dragging a facility.
                PaintRect(tex, 0, 0, 32, 2, dark);
                PaintRect(tex, 0, 0, 2, 32, dark);
                PaintRect(tex, 2, 2, 28, 1, mid);
                PaintRect(tex, 2, 2, 1, 28, mid);
                PaintRect(tex, 2, 30, 30, 2, dark);
                PaintRect(tex, 30, 2, 2, 30, dark);
                if (variant == 1 || variant == 3)
                    PaintRect(tex, 18 - variant, 13 + variant, 6, 3, mid);
                if (variant == 2)
                    PaintRect(tex, 5, 22, 8, 3, dark);

                // Fine cracks, embedded flakes and shaded grit clusters.
                DrawCrack(tex, 5 + variant * 5, 7 + (variant % 2) * 11,
                    variant % 2 == 0 ? 1 : -1, deep, light);
                PaintRect(tex, 22 - variant * 2, 6 + variant * 4, 3, 1, dark);
                PaintRect(tex, 23 - variant * 2, 7 + variant * 4, 2, 1, light);
                PaintRect(tex, 8 + variant * 4, 24 - variant * 3, 2, 2, dark);
                tex.SetPixel(9 + variant * 4, 25 - variant * 3, light);
                PaintRect(tex, 26 - variant * 3, 26, 2, 1, mid);
            }
            tex.Apply();
            return tex;
        }

        private static void PaintRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            for (int px = x; px < x + width; px++)
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    tex.SetPixel(px, py, color);
        }

        private static void DrawCrack(Texture2D tex, int x, int y, int direction, Color shadow, Color rim)
        {
            int[] steps = { 0, 0, 1, 1, 2, 1, 2, 3, 3, 4 };
            for (int i = 0; i < steps.Length; i += 2)
            {
                int px = x + steps[i] * direction;
                int py = y + steps[i + 1];
                if (px > 1 && px < tex.width - 2 && py > 1 && py < tex.height - 2)
                {
                    tex.SetPixel(px, py, shadow);
                    tex.SetPixel(px + direction, py, rim);
                }
            }
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

        private static Texture2D MakeNurseryPit()
        {
            const int w = 128, h = 96;
            var tex = NewTexture(w, h);
            Clear(tex);
            Vector2 center = new Vector2((w - 1) * .5f, (h - 1) * .5f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                Vector2 d = new Vector2((x - center.x) / 58f, (y - center.y) / 39f);
                float r = d.magnitude;
                if (r > 1f) continue;
                float grit = ((x * 11 + y * 17) % 19 == 0) ? .025f : 0f;
                Color inner = new Color(.16f + grit, .115f + grit * .6f, .07f, .92f);
                Color lip = new Color(.47f, .34f, .17f, 1f);
                Color shadow = new Color(.055f, .065f, .06f, .92f);
                Color color = r > .88f ? lip : r > .73f ? shadow : inner;
                if (r > .93f && ((x + y) % 7 < 2)) color = new Color(.64f, .43f, .18f, 1f);
                tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakePlacementGrid()
        {
            const int w = 260, h = 200;
            var tex = NewTexture(w, h);
            Clear(tex);
            var minor = new Color(1f, 1f, 1f, .22f);
            var major = new Color(1f, 1f, 1f, .52f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                bool lineX = x % 10 == 0;
                bool lineY = y % 10 == 0;
                if (!lineX && !lineY) continue;
                bool strong = x % 40 == 0 || y % 40 == 0;
                tex.SetPixel(x, y, strong ? major : minor);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeFacilityDecor(bool commandTier)
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            Clear(tex);
            var center = new Vector2(31.5f, 31.5f);
            int modules = commandTier ? 8 : 4;
            for (int i = 0; i < modules; i++)
            {
                float angle = Mathf.PI * 2f * i / modules + (commandTier ? Mathf.PI / 8f : 0f);
                Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (commandTier ? 28f : 25f);
                int half = commandTier ? 2 : 3;
                for (int y = -half; y <= half; y++) for (int x = -half; x <= half; x++)
                {
                    int px = Mathf.RoundToInt(p.x) + x;
                    int py = Mathf.RoundToInt(p.y) + y;
                    if (px < 0 || py < 0 || px >= size || py >= size) continue;
                    bool edge = Mathf.Abs(x) == half || Mathf.Abs(y) == half;
                    tex.SetPixel(px, py, edge ? new Color(0f, 0f, 0f, .85f) : Color.white);
                }
                if (commandTier)
                {
                    int tipX = Mathf.RoundToInt(center.x + Mathf.Cos(angle) * 31f);
                    int tipY = Mathf.RoundToInt(center.y + Mathf.Sin(angle) * 31f);
                    if (tipX >= 0 && tipY >= 0 && tipX < size && tipY < size)
                        tex.SetPixel(tipX, tipY, Color.white);
                }
            }
            tex.Apply();
            return tex;
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
