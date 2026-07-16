using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Gameplay;
using TCC.Managers;
using TCC.UI;

namespace TCC.EditorTools
{
    /// <summary>
    /// One-shot scaffolder: builds the prefabs and the Main scene from the scripts
    /// and data assets, wiring every serialized reference. Produces a real,
    /// editable scene (not a runtime-drawn one) so the layout can be tweaked in the
    /// editor afterwards. Run via the TCC menu or -executeMethod for batchmode.
    /// </summary>
    public static partial class SceneBootstrap
    {
        const string ART = "Assets/_Game/Art/Sprites/";
        const string PREFABS = "Assets/_Game/Prefabs/";
        const string SCENE = "Assets/_Game/Scenes/Main.unity";
        const string FONT = "Assets/TextMesh Pro/Fonts/ZKHAPPY SDF.asset";

        const string CFG_SIM = "Assets/_Game/Data/SimulationConfig.asset";
        const string CFG_ECO = "Assets/_Game/Data/EconomyConfig.asset";
        const string CFG_AUDIO = "Assets/_Game/Data/AudioLibrary.asset";
        const string CFG_LOC = "Assets/_Game/Data/LocalizationTable.asset";

        // ---- world geometry (kept in one place so scene + managers agree) ----
        static readonly Vector2 ActivityMin = new Vector2(-9.2f, -5f);
        static readonly Vector2 ActivityMax = new Vector2(1f, 5f);
        static readonly Vector2 BirthPos = new Vector2(-6.8f, -3.2f);
        static readonly Vector2 LaborPos = new Vector2(-6.8f, 3.2f);
        const float ZoneRadius = 1.7f;
        const int LaborCapacity = 10;

        static TMP_FontAsset _font;

        [MenuItem("TCC/Build Main Scene")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PREFABS);
            Directory.CreateDirectory(Path.GetDirectoryName(SCENE));

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);

            var simCfg = AssetDatabase.LoadAssetAtPath<SimulationConfig>(CFG_SIM);
            var ecoCfg = AssetDatabase.LoadAssetAtPath<EconomyConfig>(CFG_ECO);
            var audioLib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(CFG_AUDIO);
            var locTable = AssetDatabase.LoadAssetAtPath<LocalizationTable>(CFG_LOC);

            var creaturePrefab = BuildCreaturePrefab();
            var eggPrefab = BuildEggPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Camera (full screen, dark cave) ----
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            // ---- World (zones, pit, entity parent) ----
            BuildWorld(ecoCfg, out var worldRoot, out var birthCenter, out var labor);

            // ---- Managers ----
            var managers = new GameObject("[Managers]");

            NewChild(managers, "GameManager").AddComponent<GameManager>();

            var loc = NewChild(managers, "LocalizationManager").AddComponent<LocalizationManager>();
            SetRef(loc, "_table", locTable);
            SetRef(loc, "_font", _font);

            var audio = NewChild(managers, "AudioManager").AddComponent<AudioManager>();
            SetRef(audio, "_library", audioLib);

            var eco = NewChild(managers, "EconomyManager").AddComponent<EconomyManager>();
            SetRef(eco, "_config", ecoCfg);

            var simGo = NewChild(managers, "SimulationManager");
            var sim = simGo.AddComponent<SimulationManager>();
            SetRef(sim, "_config", simCfg);
            SetRef(sim, "_creaturePrefab", creaturePrefab);
            SetRef(sim, "_eggPrefab", eggPrefab);
            SetRef(sim, "_worldRoot", worldRoot);
            SetRef(sim, "_activityMin", ActivityMin);
            SetRef(sim, "_activityMax", ActivityMax);
            SetRef(sim, "_birthCenter", birthCenter);
            SetRef(sim, "_birthRadius", ZoneRadius);
            SetRef(sim, "_labor", labor);

            NewChild(managers, "SettingsManager").AddComponent<SettingsManager>();

            // ---- HUD ----
            BuildHud(out var hudView);

            var ui = NewChild(managers, "UIManager").AddComponent<UIManager>();
            SetRef(ui, "_hud", hudView);

            // ---- UI overlay system (Block / Toast / Alert / Settings / Title) ----
            BuildUISystem();

            // ---- Save ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SCENE);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TCC] Main scene built at " + SCENE);
        }

        // ================= World =================
        static void BuildWorld(EconomyConfig eco, out Transform worldRoot,
            out Transform birthCenter, out LaborZone labor)
        {
            var world = new GameObject("[World]");

            // Nursery circle (bugs are born here) — bottom-left.
            var birth = WorldCircle(world.transform, "BirthCircle", BirthPos, ZoneRadius,
                new Color(0.32f, 0.24f, 0.17f, 0.9f), -2);
            birthCenter = birth.transform;

            // Labor circle — top-left. Prime bugs dropped inside earn coins.
            var laborGo = WorldCircle(world.transform, "LaborCircle", LaborPos, ZoneRadius,
                new Color(0.17f, 0.30f, 0.24f, 0.95f), -2);
            labor = laborGo.AddComponent<LaborZone>();
            SetRef(labor, "_incomePerSec", eco != null ? eco.laborIncomePerSec : 2);
            SetRef(labor, "_capacity", LaborCapacity);
            SetRef(labor, "_radius", ZoneRadius);

            // Deep pit — vertical dark divider between the activity area and the
            // (future) right-hand spawn region.
            var pit = new GameObject("Pit");
            pit.transform.SetParent(world.transform, false);
            var psr = pit.AddComponent<SpriteRenderer>();
            psr.sprite = Spr("dish");
            psr.color = new Color(0.02f, 0.03f, 0.04f, 1f);
            psr.sortingOrder = -1;
            pit.transform.position = new Vector3(1.7f, 0f, 0f);
            pit.transform.localScale = new Vector3(1.1f, 6.6f, 1f);

            // Parent that owns every spawned bug and egg.
            var spawnRoot = new GameObject("WorldRoot");
            spawnRoot.transform.SetParent(world.transform, false);
            worldRoot = spawnRoot.transform;
        }

        static GameObject WorldCircle(Transform parent, string name, Vector2 pos,
            float radius, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("dish");
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radius; // sprite radius 1u -> radius
            return go;
        }

        // ================= Prefabs =================
        static Creature BuildCreaturePrefab()
        {
            var go = new GameObject("Creature");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("micro");
            sr.sortingOrder = 5;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            var c = go.AddComponent<Creature>();
            SetRef(c, "_infantSprite", Spr("micro"));
            SetRef(c, "_adultSprite", Spr("micromax"));
            SetRef(c, "_elderSprite", Spr("elder"));
            go.transform.localScale = Vector3.one * 0.68f; // ~68px small circle
            var prefab = SavePrefab(go, PREFABS + "Creature.prefab");
            return prefab.GetComponent<Creature>();
        }

        static Egg BuildEggPrefab()
        {
            var go = new GameObject("Egg");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("egg");
            sr.sortingOrder = 4;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.55f;
            go.AddComponent<Egg>();
            go.transform.localScale = Vector3.one * 0.6f;
            var prefab = SavePrefab(go, PREFABS + "Egg.prefab");
            return prefab.GetComponent<Egg>();
        }

        // ================= HUD =================
        static void BuildHud(out HudView hudView)
        {
            var canvasGo = new GameObject("HUD Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            hudView = canvasGo.AddComponent<HudView>();

            // ---- Compact HUD card, tucked into the bottom-right corner ----
            // (the rest of the right side is left free for the future spawn region)
            var panelGo = new GameObject("HudCard", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var prt = (RectTransform)panelGo.transform;
            prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 0);
            prt.pivot = new Vector2(1, 0);
            prt.sizeDelta = new Vector2(480, 330);
            prt.anchoredPosition = new Vector2(-24, 24);
            panelGo.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.15f, 0.92f);
            var panel = panelGo.transform;

            // Money
            var money = Text(panel, "Money", 42,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -22), new Vector2(440, 60), TextAlignmentOptions.Center);
            money.text = "金币：--";
            money.color = new Color(1f, 0.86f, 0.4f);

            // Population
            var pop = Text(panel, "Population", 30,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -92), new Vector2(440, 44), TextAlignmentOptions.Center);
            pop.text = "幼体 -- · 成体 --";
            pop.color = new Color(0.8f, 0.85f, 0.9f);

            // Buy button
            var buyBtn = MakeButton(panel, "BuyButton",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -150), new Vector2(320, 74),
                new Color(0.25f, 0.5f, 0.35f, 1f), 34, out var buyLabel);
            Localize(buyLabel, LocalizationTable.Keys.HudBuy);

            // Pause + Language (bottom row of the card)
            var pauseBtn = MakeButton(panel, "PauseButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-108, 22), new Vector2(200, 64),
                new Color(0.2f, 0.45f, 0.55f, 1f), 30, out var pauseLabel);
            Localize(pauseLabel, LocalizationTable.Keys.Pause);

            var langBtn = MakeButton(panel, "LanguageButton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(108, 22), new Vector2(200, 64),
                new Color(0.2f, 0.45f, 0.55f, 1f), 30, out var langLabel);
            Localize(langLabel, LocalizationTable.Keys.LanguageToggle);

            // Wire HudView
            SetRef(hudView, "_moneyText", money);
            SetRef(hudView, "_populationText", pop);
            SetRef(hudView, "_buyButton", buyBtn);
            SetRef(hudView, "_languageButton", langBtn);
            SetRef(hudView, "_pauseButton", pauseBtn);
            SetRef(hudView, "_pauseLabel", pauseLabel.GetComponent<LocalizedText>());
        }

        // ================= helpers =================
        static Sprite Spr(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ART + name + ".png");

        static GameObject NewChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static TextMeshProUGUI Text(Transform parent, string name, float size,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = size;
            t.alignment = align;
            t.raycastTarget = false;
            t.color = Color.white;
            var rt = t.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            return t;
        }

        static Button MakeButton(Transform parent, string name,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size,
            Color color, float fontSize, out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            label = Text(go.transform, "Label", fontSize,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        static void Localize(TextMeshProUGUI t, string key)
        {
            var lt = t.gameObject.AddComponent<LocalizedText>();
            SetRef(lt, "_key", key);
        }

        static void SetRef(Object comp, string prop, object value)
        {
            var so = new SerializedObject(comp);
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogError($"[TCC] property '{prop}' not found on {comp.GetType().Name}");
                return;
            }
            switch (value)
            {
                case string s: p.stringValue = s; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case Vector2 v: p.vector2Value = v; break;
                case Object o: p.objectReferenceValue = o; break;
                default: p.objectReferenceValue = value as Object; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
