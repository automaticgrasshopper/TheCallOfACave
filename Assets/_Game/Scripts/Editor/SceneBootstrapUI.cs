using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TCC.Data;
using TCC.UI;

namespace TCC.EditorTools
{
    /// <summary>
    /// Second half of the scaffolder: builds the always-active [UI] root and the five
    /// overlay panels (Block / Toast / Alert / Settings / TitleMenu). Every panel is
    /// its own screen-space Canvas that starts disabled (closed); the shared UIPanel
    /// base toggles it. Widgets are made with the built-in UGUI/TMP factories so they
    /// carry Unity's default skin, then re-fonted to ZKHAPPY and wired by reference.
    /// </summary>
    public static partial class SceneBootstrap
    {
        // Stacking order: menu underneath, curtain on top of everything.
        const int SORT_TITLE = 200;
        const int SORT_SETTINGS = 300;
        const int SORT_ALERT = 500;
        const int SORT_TOAST = 800;
        const int SORT_BLOCK = 900;

        static void BuildUISystem()
        {
            var root = new GameObject("[UI]"); // stays active; panels flip their own Canvas

            BuildBlock(root.transform);
            BuildToast(root.transform);
            BuildAlert(root.transform);
            BuildSettings(root.transform);
            BuildTitleMenu(root.transform);
        }

        // ================= panels =================
        static void BuildBlock(Transform root)
        {
            var go = Panel(root, "BlockCanvas", SORT_BLOCK, out var canvas, out var group);
            var view = go.AddComponent<BlockView>();
            var img = FullImage(go.transform, "Curtain", new Color(0.04f, 0.05f, 0.06f, 1f));
            img.raycastTarget = true;
            WirePanel(view, canvas, group, blocksInput: true);
        }

        static void BuildToast(Transform root)
        {
            var go = Panel(root, "ToastCanvas", SORT_TOAST, out var canvas, out var group);
            var view = go.AddComponent<ToastView>();

            var box = Box(go.transform, new Vector2(0.5f, 0f), new Vector2(0, 140),
                new Vector2(720, 96), new Color(0.06f, 0.08f, 0.10f, 0.92f));
            var txt = Text(box.transform, "Text", 38,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680, 80), TextAlignmentOptions.Center);
            txt.color = new Color(0.95f, 0.95f, 0.92f);
            txt.text = "…";

            SetRef(view, "_text", txt);
            WirePanel(view, canvas, group, blocksInput: false); // a toast never eats clicks
        }

        static void BuildAlert(Transform root)
        {
            var go = Panel(root, "AlertCanvas", SORT_ALERT, out var canvas, out var group);
            var view = go.AddComponent<AlertView>();

            var dim = FullImage(go.transform, "Dim", new Color(0, 0, 0, 0.55f));
            dim.raycastTarget = true;

            var box = Box(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(680, 380), new Color(0.12f, 0.14f, 0.17f, 1f));

            var title = Text(box.transform, "Title", 46,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(620, 70), TextAlignmentOptions.Center);
            title.color = new Color(0.96f, 0.93f, 0.82f);

            var msg = Text(box.transform, "Message", 34,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(600, 120), TextAlignmentOptions.Center);
            msg.color = new Color(0.85f, 0.88f, 0.9f);

            var confirm = MenuButton(box.transform, "ConfirmButton", new Vector2(-150, -120),
                new Vector2(240, 82), LocalizationTable.Keys.AlertConfirm, out _);
            var cancel = MenuButton(box.transform, "CancelButton", new Vector2(150, -120),
                new Vector2(240, 82), LocalizationTable.Keys.AlertCancel, out _);
            cancel.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.34f, 1f);

            SetRef(view, "_title", title);
            SetRef(view, "_message", msg);
            SetRef(view, "_confirmButton", confirm);
            SetRef(view, "_cancelButton", cancel);
            WirePanel(view, canvas, group, blocksInput: true);
        }

        static void BuildSettings(Transform root)
        {
            var go = Panel(root, "SettingsCanvas", SORT_SETTINGS, out var canvas, out var group);
            var view = go.AddComponent<SettingsView>();

            var dim = FullImage(go.transform, "Dim", new Color(0, 0, 0, 0.6f));
            dim.raycastTarget = true;

            var box = Box(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(760, 760), new Color(0.12f, 0.14f, 0.17f, 1f));

            var title = Text(box.transform, "Title", 50,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 310), new Vector2(680, 70), TextAlignmentOptions.Center);
            Localize(title, LocalizationTable.Keys.SettingsTitle);
            title.color = new Color(0.96f, 0.93f, 0.82f);

            const float lx = -300f, wx = 130f, ww = 320f;

            RowLabel(box.transform, 200, LocalizationTable.Keys.SettingsFullscreen, lx);
            var fs = MakeToggle(box.transform, new Vector2(wx - 130, 200));

            RowLabel(box.transform, 105, LocalizationTable.Keys.SettingsResolution, lx);
            var res = MakeDropdown(box.transform, new Vector2(wx, 105), ww);

            RowLabel(box.transform, 10, LocalizationTable.Keys.SettingsMusic, lx);
            var music = MakeSlider(box.transform, new Vector2(wx, 10), ww);

            RowLabel(box.transform, -85, LocalizationTable.Keys.SettingsSfx, lx);
            var sfx = MakeSlider(box.transform, new Vector2(wx, -85), ww);

            RowLabel(box.transform, -180, LocalizationTable.Keys.SettingsLanguage, lx);
            var lang = MakeDropdown(box.transform, new Vector2(wx, -180), ww);

            var back = MenuButton(box.transform, "BackButton", new Vector2(0, -300),
                new Vector2(300, 84), LocalizationTable.Keys.SettingsBack, out _);

            SetRef(view, "_fullscreenToggle", fs);
            SetRef(view, "_resolutionDropdown", res);
            SetRef(view, "_musicSlider", music);
            SetRef(view, "_sfxSlider", sfx);
            SetRef(view, "_languageDropdown", lang);
            SetRef(view, "_backButton", back);
            WirePanel(view, canvas, group, blocksInput: true);
        }

        static void BuildTitleMenu(Transform root)
        {
            var go = Panel(root, "TitleMenuCanvas", SORT_TITLE, out var canvas, out var group);
            var view = go.AddComponent<TitleMenuView>();

            var dim = FullImage(go.transform, "Dim", new Color(0.03f, 0.04f, 0.05f, 0.85f));
            dim.raycastTarget = true;

            var box = Box(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(600, 680), new Color(0.10f, 0.12f, 0.15f, 0.96f));

            var title = Text(box.transform, "Title", 60,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 250), new Vector2(560, 90), TextAlignmentOptions.Center);
            Localize(title, LocalizationTable.Keys.GameTitle);
            title.color = new Color(0.96f, 0.93f, 0.82f);

            var start = MenuButton(box.transform, "StartButton", new Vector2(0, 110),
                new Vector2(440, 88), LocalizationTable.Keys.MenuStart, out var startLabel);
            var cont = MenuButton(box.transform, "ContinueButton", new Vector2(0, 10),
                new Vector2(440, 88), LocalizationTable.Keys.MenuContinue, out _);
            var settings = MenuButton(box.transform, "SettingsButton", new Vector2(0, -90),
                new Vector2(440, 88), LocalizationTable.Keys.MenuSettings, out _);
            var quit = MenuButton(box.transform, "QuitButton", new Vector2(0, -190),
                new Vector2(440, 88), LocalizationTable.Keys.MenuQuit, out _);
            quit.GetComponent<Image>().color = new Color(0.42f, 0.22f, 0.24f, 1f);

            SetRef(view, "_startButton", start);
            SetRef(view, "_startLabel", startLabel);
            SetRef(view, "_continueButton", cont);
            SetRef(view, "_settingsButton", settings);
            SetRef(view, "_quitButton", quit);
            WirePanel(view, canvas, group, blocksInput: true);
        }

        // ================= UI helpers =================
        static GameObject Panel(Transform root, string name, int sortOrder,
            out Canvas canvas, out CanvasGroup group)
        {
            var go = new GameObject(name,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            go.transform.SetParent(root, false);

            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrder;
            canvas.enabled = false; // closed by default

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return go;
        }

        static void WirePanel(Component view, Canvas canvas, CanvasGroup group, bool blocksInput)
        {
            SetRef(view, "_canvas", canvas);
            SetRef(view, "_group", group);
            SetBool(view, "_blocksInput", blocksInput);
        }

        static Image FullImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static Image Box(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static void RowLabel(Transform box, float y, string key, float x)
        {
            var t = Text(box, "Label", 34,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(320, 56), TextAlignmentOptions.Left);
            Localize(t, key);
            t.color = new Color(0.85f, 0.88f, 0.9f);
        }

        static Button MenuButton(Transform parent, string name, Vector2 pos, Vector2 size,
            string labelKey, out LocalizedText label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.20f, 0.45f, 0.55f, 1f);

            var txt = Text(go.transform, "Label", 38,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            var lrt = txt.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            Localize(txt, labelKey);
            label = txt.GetComponent<LocalizedText>();
            return go.GetComponent<Button>();
        }

        static Slider MakeSlider(Transform parent, Vector2 pos, float width)
        {
            var go = DefaultControls.CreateSlider(UIResources());
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(width, 26);
            var s = go.GetComponent<Slider>();
            s.minValue = 0f; s.maxValue = 1f; s.value = 0.8f;
            return s;
        }

        static Toggle MakeToggle(Transform parent, Vector2 pos)
        {
            var go = DefaultControls.CreateToggle(UIResources());
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(48, 48);
            var lbl = go.transform.Find("Label");
            if (lbl != null) Object.DestroyImmediate(lbl.gameObject);
            var bg = go.transform.Find("Background") as RectTransform;
            if (bg != null) bg.sizeDelta = new Vector2(48, 48);
            return go.GetComponent<Toggle>();
        }

        static TMP_Dropdown MakeDropdown(Transform parent, Vector2 pos, float width)
        {
            var go = TMP_DefaultControls.CreateDropdown(TMPResources());
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(width, 52);
            var d = go.GetComponent<TMP_Dropdown>();
            if (_font != null)
                foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                    t.font = _font;
            return d;
        }

        static DefaultControls.Resources UIResources()
        {
            return new DefaultControls.Resources
            {
                standard = Builtin("UI/Skin/UISprite.psd"),
                background = Builtin("UI/Skin/Background.psd"),
                inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
                knob = Builtin("UI/Skin/Knob.psd"),
                checkmark = Builtin("UI/Skin/Checkmark.psd"),
                dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
                mask = Builtin("UI/Skin/UIMask.psd"),
            };
        }

        static TMP_DefaultControls.Resources TMPResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = Builtin("UI/Skin/UISprite.psd"),
                background = Builtin("UI/Skin/Background.psd"),
                inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
                knob = Builtin("UI/Skin/Knob.psd"),
                checkmark = Builtin("UI/Skin/Checkmark.psd"),
                dropdown = Builtin("UI/Skin/DropdownArrow.psd"),
                mask = Builtin("UI/Skin/UIMask.psd"),
            };
        }

        static Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static void SetBool(Object comp, string prop, bool value)
        {
            var so = new SerializedObject(comp);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[TCC] bool property '{prop}' not found on {comp.GetType().Name}"); return; }
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
