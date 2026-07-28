using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Dice;

namespace BG3DiceSystem.UI
{
    public class DiceButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region Events
        public event Action<DiceType, DiceButtonUI> OnClicked;
        #endregion

        #region Inspector Fields
        [Header("Button Components")]
        public DiceType DiceType;
        public Image BackgroundImage;
        public TextMeshProUGUI LabelText;
        #endregion

        #region Private Fields
        private bool _isSelected;
        private bool _isInitialized;
        #endregion

        #region Initialization
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (BackgroundImage == null) BackgroundImage = GetComponent<Image>();
            if (LabelText == null) LabelText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Awake()
        {
            Initialize();
        }
        #endregion

        #region Public Selection API
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            Debug.Log($"[DiceButtonUI] Button '{gameObject.name}' ({DiceType}) SetSelected: {isSelected}");
            AnimateState();
        }
        #endregion

        #region Pointer Event Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected)
            {
                if (Application.isPlaying)
                {
                    transform.DOScale(Vector3.one * 1.08f, 0.15f, Ease.OutQuad);
                }
                if (BackgroundImage != null)
                {
                    BackgroundImage.color = new Color(0.28f, 0.28f, 0.35f, 0.95f);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[DiceButtonUI] Button '{gameObject.name}' clicked for DiceType: {DiceType}");
            if (Application.isPlaying)
            {
                transform.DOPunchScale(Vector3.one * 0.15f, 0.2f);
            }
            OnClicked?.Invoke(DiceType, this);
        }
        #endregion

        #region Visual State Animation
        private void AnimateState()
        {
            if (_isSelected)
            {
                if (Application.isPlaying)
                {
                    transform.DOScale(Vector3.one * 1.15f, 0.2f, Ease.OutBack);
                }
                else
                {
                    transform.localScale = Vector3.one * 1.15f;
                }
                if (BackgroundImage != null)
                {
                    BackgroundImage.color = new Color(0.95f, 0.8f, 0.35f, 1f);
                }
                if (LabelText != null)
                {
                    LabelText.color = new Color(0.12f, 0.1f, 0.05f, 1f);
                }
            }
            else
            {
                if (Application.isPlaying)
                {
                    transform.DOScale(Vector3.one, 0.2f, Ease.OutQuad);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
                if (BackgroundImage != null)
                {
                    BackgroundImage.color = new Color(0.18f, 0.18f, 0.22f, 0.85f);
                }
                if (LabelText != null)
                {
                    LabelText.color = Color.white;
                }
            }
        }
        #endregion
    }
}
