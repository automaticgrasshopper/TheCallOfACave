using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TCC.Core;
using TCC.Data;
using TCC.Managers;

namespace TCC.UI
{
    /// <summary>Extinction screen built at runtime so every saved scene gets the
    /// loss flow without manual prefab wiring.</summary>
    public class GameOverView : UIPanel<GameOverView>
    {
        private Text _title;
        private Text _body;
        private Text _restartLabel;
        private Text _menuLabel;
        private Button _restartButton;
        private Button _menuButton;

        public static void Ensure()
        {
            if (Exists) return;
            var root = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 700;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Image dim = Image(root.transform, "ExtinctionVeil", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(.008f, .012f, .016f, .92f));
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            Image card = Image(root.transform, "ColonyLostCard", new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(760, 510), new Color(.045f, .075f, .09f, .98f));
            AddOutline(card.gameObject, new Color(.91f, .58f, .18f, .8f), new Vector2(3, -3));

            Text title = Label(card.transform, "Title", new Vector2(0, 140), new Vector2(680, 90), 48,
                new Color(1f, .72f, .27f), TextAnchor.MiddleCenter);
            Text body = Label(card.transform, "Body", new Vector2(0, 38), new Vector2(620, 130), 25,
                new Color(.79f, .87f, .86f), TextAnchor.MiddleCenter);
            Button restart = Button(card.transform, "RebuildButton", new Vector2(0, -105), new Color(.08f, .44f, .48f, 1f));
            Text restartLabel = Label(restart.transform, "Label", Vector2.zero, new Vector2(390, 68), 25, Color.white, TextAnchor.MiddleCenter);
            Button menu = Button(card.transform, "TitleButton", new Vector2(0, -195), new Color(.18f, .22f, .25f, 1f));
            Text menuLabel = Label(menu.transform, "Label", Vector2.zero, new Vector2(390, 68), 22, new Color(.84f, .87f, .86f), TextAnchor.MiddleCenter);

            var view = root.AddComponent<GameOverView>();
            view._title = title; view._body = body;
            view._restartLabel = restartLabel; view._menuLabel = menuLabel;
            view._restartButton = restart; view._menuButton = menu;
            view.BindButtons();
        }

        protected override void OnInit()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void BindButtons()
        {
            _restartButton.onClick.AddListener(Restart);
            _menuButton.onClick.AddListener(ReturnToTitle);
        }

        protected override void OnDestroy()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            base.OnDestroy();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver) Show();
        }

        protected override void OnShow()
        {
            if (!LocalizationManager.Exists) return;
            var loc = LocalizationManager.Instance;
            _title.text = loc.Get(LocalizationTable.Keys.GameOverTitle);
            string duration = GameManager.Exists
                ? GameManager.FormatSessionTime(GameManager.Instance.SessionSeconds) : "00:00";
            string year = GameManager.Exists ? GameManager.Instance.FormatColonyYear() : "0001";
            _body.text = loc.Get(LocalizationTable.Keys.GameOverBody) + "\n\n" + string.Format(
                loc.Get(LocalizationTable.Keys.GameOverDuration), duration, year);
            _restartLabel.text = loc.Get(LocalizationTable.Keys.GameOverRestart);
            _menuLabel.text = loc.Get(LocalizationTable.Keys.GameOverMenu);
        }

        private void Restart() { GameManager.BootIntoPlay = true; Reload(); }
        private void ReturnToTitle() { GameManager.BootIntoPlay = false; Reload(); }

        private void Reload()
        {
            Time.timeScale = 1f;
            if (BlockView.Exists)
                BlockView.Instance.Transition(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static Image Image(Transform parent, string name, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color;
            return image;
        }

        private static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            // Unity 2022 no longer exposes Arial.ttf as a built-in resource.
            // LegacyRuntime.ttf is the supported compatibility font for UGUI Text.
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.color = color; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, .8f);
            return text;
        }

        private static Button Button(Transform parent, string name, Vector2 position, Color color)
        {
            Image image = Image(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(400, 70), color);
            AddOutline(image.gameObject, new Color(.94f, .68f, .25f, .45f), new Vector2(1, -1));
            return image.gameObject.AddComponent<Button>();
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color; outline.effectDistance = distance;
        }
    }
}
