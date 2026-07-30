using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Utilities.Tweening;

namespace BG3DiceSystem.UI
{
    /// <summary>
    /// Static Top-Left Quit Game Button component with full localization support.
    /// </summary>
    public class QuitButtonUI : MonoBehaviour
    {
        public Button QuitButton;
        public Image BackgroundImage;
        public Outline ButtonOutline;
        public TextMeshProUGUI ButtonText;

        private ILocalizationService _localizationService;
        private IAudioService _audioService;

        public void Initialize(ILocalizationService localizationService, IAudioService audioService)
        {
            _localizationService = localizationService;
            _audioService = audioService;

            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= UpdateVisuals;
                _localizationService.OnLanguageChanged += UpdateVisuals;
                UpdateVisuals();
            }

            BindListeners();
        }

        private void Awake()
        {
            BindListeners();
        }

        private void OnDestroy()
        {
            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= UpdateVisuals;
            }
        }

        public void UpdateVisuals()
        {
            if (ButtonText != null)
            {
                string text = _localizationService != null ? _localizationService.GetText("quit_button") : "QUIT";
                ButtonText.text = text;
            }
        }

        private void BindListeners()
        {
            if (QuitButton == null) QuitButton = GetComponentInChildren<Button>();
            if (QuitButton != null)
            {
                QuitButton.onClick.RemoveAllListeners();
                QuitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnQuitClicked()
        {
            if (QuitButton != null)
            {
                QuitButton.transform.DOKill();
                QuitButton.transform.localScale = Vector3.one;
                QuitButton.transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.2f);
            }

            _audioService?.PlayButtonClick();
            Debug.Log("[QuitButtonUI] Quit Game requested by user.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
