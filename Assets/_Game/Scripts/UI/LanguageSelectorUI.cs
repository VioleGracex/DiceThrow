using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Utilities.Tweening;

namespace BG3DiceSystem.UI
{
    /// <summary>
    /// Top-Right Language Switcher UI featuring EN and RU toggle buttons.
    /// Styled with a sleek BG3-inspired dark fantasy aesthetic.
    /// </summary>
    public class LanguageSelectorUI : MonoBehaviour
    {
        [Header("UI Component References")]
        public Button EnButton;
        public Image EnBackground;
        public Outline EnOutline;
        public TextMeshProUGUI EnText;

        public Button RuButton;
        public Image RuBackground;
        public Outline RuOutline;
        public TextMeshProUGUI RuText;

        private ILocalizationService _localizationService;
        private IAudioService _audioService;
        private bool _isInitialized;

        public void Initialize(ILocalizationService localizationService, IAudioService audioService)
        {
            _localizationService = localizationService;
            _audioService = audioService;

            EnsureUIReferences();
            BindListeners();

            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= UpdateVisuals;
                _localizationService.OnLanguageChanged += UpdateVisuals;
                UpdateVisuals();
            }

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= UpdateVisuals;
            }
        }

        private void BindListeners()
        {
            if (EnButton != null)
            {
                EnButton.onClick.RemoveAllListeners();
                EnButton.onClick.AddListener(() =>
                {
                    AnimateButtonClick(EnButton.transform);
                    _audioService?.PlayButtonClick();
                    _localizationService?.SetLanguage(Language.EN);
                });
            }

            if (RuButton != null)
            {
                RuButton.onClick.RemoveAllListeners();
                RuButton.onClick.AddListener(() =>
                {
                    AnimateButtonClick(RuButton.transform);
                    _audioService?.PlayButtonClick();
                    _localizationService?.SetLanguage(Language.RU);
                });
            }
        }

        public void UpdateVisuals()
        {
            if (_localizationService == null) return;

            bool isEn = _localizationService.CurrentLanguage == Language.EN;
            bool isRu = _localizationService.CurrentLanguage == Language.RU;

            // EN Button Styling
            if (EnBackground != null) EnBackground.color = isEn ? new Color(0.22f, 0.22f, 0.30f, 0.95f) : new Color(0.12f, 0.12f, 0.16f, 0.85f);
            if (EnOutline != null) EnOutline.effectColor = isEn ? new Color(0.95f, 0.78f, 0.35f, 1f) : new Color(0.35f, 0.35f, 0.45f, 0.4f);
            if (EnText != null) EnText.color = isEn ? new Color(0.98f, 0.88f, 0.55f, 1f) : new Color(0.70f, 0.70f, 0.75f, 0.8f);

            // RU Button Styling
            if (RuBackground != null) RuBackground.color = isRu ? new Color(0.22f, 0.22f, 0.30f, 0.95f) : new Color(0.12f, 0.12f, 0.16f, 0.85f);
            if (RuOutline != null) RuOutline.effectColor = isRu ? new Color(0.95f, 0.78f, 0.35f, 1f) : new Color(0.35f, 0.35f, 0.45f, 0.4f);
            if (RuText != null) RuText.color = isRu ? new Color(0.98f, 0.88f, 0.55f, 1f) : new Color(0.70f, 0.70f, 0.75f, 0.8f);
        }

        private void AnimateButtonClick(Transform target)
        {
            if (target != null)
            {
                target.DOKill();
                target.localScale = Vector3.one;
                target.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.2f);
            }
        }

        private void EnsureUIReferences()
        {
            if (EnButton != null && RuButton != null) return;

            // Auto-create UI layout if references are missing
            RectTransform rect = transform as RectTransform;
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(110f, 36f);

            var hlg = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            if (EnButton == null)
            {
                var (btn, bg, outl, tmp) = CreatePillButton("Btn_EN", "EN", transform);
                EnButton = btn; EnBackground = bg; EnOutline = outl; EnText = tmp;
            }

            if (RuButton == null)
            {
                var (btn, bg, outl, tmp) = CreatePillButton("Btn_RU", "RU", transform);
                RuButton = btn; RuBackground = bg; RuOutline = outl; RuText = tmp;
            }
        }

        private (Button btn, Image bg, Outline outl, TextMeshProUGUI tmp) CreatePillButton(string name, string label, Transform parent)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(48f, 32f);

            Image bg = btnObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            Outline outl = btnObj.GetComponent<Outline>();
            outl.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.85f);
            outl.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return (btn, bg, outl, tmp);
        }
    }
}
