using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GW.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class MainMenuLayoutBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string titleText = "GOLDEN WRAP";

        [SerializeField]
        private string subtitleText = "Бесконечная фабрика золотой обёртки";

        [SerializeField]
        private string playButtonText = "Играть";

        [SerializeField]
        private string showcaseTitleText = "Витрина узоров";

        [SerializeField]
        private string showcaseSubtitleText = "Здесь появятся ваши любимые паттерны фольги.";

        private Button cachedPlayButton;

        private static readonly Color32 BackgroundColor = new Color32(14, 13, 12, 230);
        private static readonly Color32 PanelColor = new Color32(54, 46, 36, 200);
        private static readonly Color32 TitleColor = new Color32(0xF3, 0xE9, 0xD2, 0xFF);
        private static readonly Color32 SubtitleColor = new Color32(0xF9, 0xF9, 0xF9, 180);
        private static readonly Color32 ButtonNormalColor = new Color32(0xC9, 0xA6, 0x46, 0xFF);
        private static readonly Color32 ButtonHighlightedColor = new Color32(0xFF, 0xD3, 0x6E, 0xFF);
        private static readonly Color32 ButtonPressedColor = new Color32(0x8F, 0x7A, 0x2F, 0xFF);
        private static readonly Color32 ButtonTextColor = new Color32(0x36, 0x2E, 0x24, 0xFF);

        private void Awake()
        {
            Bootstrap();
        }

        private void OnEnable()
        {
            Bootstrap();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Bootstrap();
        }
#endif

        private void Bootstrap()
        {
            EnsureCanvasComponents();
            EnsureEventSystem();
            EnsureLayout();
        }

        private void EnsureCanvasComponents()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            standaloneModule.forceModuleActive = false;
        }

        private void EnsureLayout()
        {
            var layoutRoot = transform.Find("LayoutRoot") as RectTransform;
            if (layoutRoot == null)
            {
                layoutRoot = CreateLayoutHierarchy();
            }

            if (layoutRoot == null)
            {
                return;
            }

            cachedPlayButton = layoutRoot.GetComponentInChildren<Button>(true);
            ConfigurePlayButton();
        }

        private RectTransform CreateLayoutHierarchy()
        {
            var layoutRoot = new GameObject("LayoutRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            layoutRoot.SetParent(transform, false);
            layoutRoot.anchorMin = Vector2.zero;
            layoutRoot.anchorMax = Vector2.one;
            layoutRoot.offsetMin = new Vector2(40f, 40f);
            layoutRoot.offsetMax = new Vector2(-40f, -40f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(layoutRoot, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = BackgroundColor;
            backgroundImage.raycastTarget = false;

            var title = CreateText("Title", layoutRoot, titleText, 44, TextAnchor.UpperLeft, TitleColor);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0.6f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(24f, -24f);

            var subtitle = CreateText("Subtitle", layoutRoot, subtitleText, 24, TextAnchor.UpperLeft, SubtitleColor);
            var subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 1f);
            subtitleRect.anchorMax = new Vector2(0.6f, 1f);
            subtitleRect.pivot = new Vector2(0f, 1f);
            subtitleRect.anchoredPosition = new Vector2(24f, -84f);

            cachedPlayButton = CreatePlayButton(layoutRoot);

            var showcase = new GameObject("ShowcasePlaceholder", typeof(RectTransform), typeof(Image));
            showcase.transform.SetParent(layoutRoot, false);
            var showcaseRect = showcase.GetComponent<RectTransform>();
            showcaseRect.anchorMin = new Vector2(0.55f, 0.15f);
            showcaseRect.anchorMax = new Vector2(0.95f, 0.85f);
            showcaseRect.offsetMin = Vector2.zero;
            showcaseRect.offsetMax = Vector2.zero;
            var showcaseImage = showcase.GetComponent<Image>();
            showcaseImage.color = PanelColor;
            showcaseImage.raycastTarget = false;

            var showcaseTitle = CreateText("ShowcaseTitle", showcase.transform, showcaseTitleText, 28, TextAnchor.UpperCenter, TitleColor);
            var showcaseTitleRect = showcaseTitle.rectTransform;
            showcaseTitleRect.anchorMin = new Vector2(0.1f, 0.8f);
            showcaseTitleRect.anchorMax = new Vector2(0.9f, 0.95f);
            showcaseTitleRect.offsetMin = Vector2.zero;
            showcaseTitleRect.offsetMax = Vector2.zero;

            var showcaseSubtitle = CreateText("ShowcaseSubtitle", showcase.transform, showcaseSubtitleText, 18, TextAnchor.MiddleCenter, SubtitleColor);
            var showcaseSubtitleRect = showcaseSubtitle.rectTransform;
            showcaseSubtitleRect.anchorMin = new Vector2(0.1f, 0.15f);
            showcaseSubtitleRect.anchorMax = new Vector2(0.9f, 0.75f);
            showcaseSubtitleRect.offsetMin = Vector2.zero;
            showcaseSubtitleRect.offsetMax = Vector2.zero;

            return layoutRoot;
        }

        private Button CreatePlayButton(Transform parent)
        {
            var buttonObject = new GameObject("PlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(240f, 72f);

            var image = buttonObject.GetComponent<Image>();
            image.color = ButtonNormalColor;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = ButtonNormalColor;
            colors.highlightedColor = ButtonHighlightedColor;
            colors.pressedColor = ButtonPressedColor;
            colors.selectedColor = ButtonHighlightedColor;
            colors.disabledColor = new Color32(80, 70, 55, 180);
            button.colors = colors;

            var label = CreateText("Label", button.transform, playButtonText, 28, TextAnchor.MiddleCenter, ButtonTextColor);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 12f);
            labelRect.offsetMax = new Vector2(-16f, -12f);

            return button;
        }

        private Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var textComponent = go.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            textComponent.raycastTarget = false;
            textComponent.supportRichText = false;

            return textComponent;
        }

        private void ConfigurePlayButton()
        {
            if (cachedPlayButton == null)
            {
                return;
            }

            cachedPlayButton.onClick.RemoveListener(OnPlayButtonClicked);
            cachedPlayButton.onClick.AddListener(OnPlayButtonClicked);

            var label = cachedPlayButton.transform.Find("Label");
            if (label != null && label.TryGetComponent<Text>(out var labelText))
            {
                labelText.text = playButtonText;
            }
        }

        private void OnPlayButtonClicked()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            var controller = GetComponent<MainMenuController>();
            if (controller != null)
            {
                controller.StartCareer();
            }
            else
            {
                Debug.LogWarning("MainMenuController component not found on the main menu root.");
            }
        }
    }
}
