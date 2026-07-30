using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Skills;

namespace BG3DiceSystem.UI
{
    public class ModifierCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<ModifierData> OnDeleteRequested;
        public event Action<ModifierData, int> OnAdjustValueRequested;

        [Header("UI Elements")]
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI ValueText;
        public Button MinusButton;
        public Button PlusButton;
        public Button DeleteButton;
        public CanvasGroup DeleteButtonCanvasGroup;
        public Image CardBackground;
        public Outline CardOutline;

        [Header("Animation Settings")]
        public float FadeDuration = 0.2f;
        public Vector3 HoverScale = new Vector3(1.03f, 1.03f, 1f);

        public ModifierData Data { get; private set; }
        public bool IsHovered { get; private set; }

        private Vector3 _originalScale = Vector3.one;

        public void Initialize(ModifierData data, ILocalizationService localizationService = null)
        {
            Data = data;
            _originalScale = transform.localScale;

            if (NameText != null)
            {
                string rawName = string.IsNullOrEmpty(data.Name) ? "Modifier" : data.Name;
                NameText.text = localizationService != null ? localizationService.GetModifierName(rawName) : rawName;
            }

            if (ValueText != null)
            {
                string sign = data.Value > 0 ? "+" : "";
                ValueText.text = $"{sign}{data.Value}";
            }

            if (MinusButton != null)
            {
                MinusButton.onClick.RemoveAllListeners();
                MinusButton.onClick.AddListener(() => {
                    MinusButton.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.15f);
                    OnAdjustValueRequested?.Invoke(Data, -1);
                });
            }

            if (PlusButton != null)
            {
                PlusButton.onClick.RemoveAllListeners();
                PlusButton.onClick.AddListener(() => {
                    PlusButton.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.15f);
                    OnAdjustValueRequested?.Invoke(Data, 1);
                });
            }

            if (DeleteButton != null)
            {
                DeleteButton.onClick.RemoveAllListeners();
                DeleteButton.onClick.AddListener(HandleDeleteClicked);
                DeleteButton.gameObject.SetActive(data.IsRemovable);
            }

            // Always keep delete button visible and interactable without whole-card hover
            if (DeleteButtonCanvasGroup != null)
            {
                DeleteButtonCanvasGroup.alpha = 1f;
                DeleteButtonCanvasGroup.interactable = true;
                DeleteButtonCanvasGroup.blocksRaycasts = true;
            }

            ConfigureStyling();
        }

        private void ConfigureStyling()
        {
            if (CardBackground != null)
            {
                CardBackground.color = new Color(0.16f, 0.16f, 0.22f, 0.95f);
            }
            if (CardOutline != null)
            {
                CardOutline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.4f);
                CardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Card background stays static (no whole-card scale or glow)
            IsHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
        }

        private void HandleDeleteClicked()
        {
            if (Data == null || !Data.IsRemovable) return;

            // Micro punch on delete click
            transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0f), 0.15f);
            OnDeleteRequested?.Invoke(Data);
        }

        private void OnDestroy()
        {
            if (DeleteButton != null)
            {
                DeleteButton.onClick.RemoveAllListeners();
            }
        }
    }
}
