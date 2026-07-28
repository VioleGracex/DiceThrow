using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.UI
{
    public class SkillCheckView : MonoBehaviour
    {
        #region Events
        public event Action<int> OnSkillSelected;
        public event Action<int> OnModifierAdjusted;
        public event Action<RollMode> OnModeChanged;
        public event Action<DiceType> OnDiceTypeSelected;
        public event Action OnRollClicked;
        #endregion

        #region Inspector Fields - Top Area & Dice Buttons
        [Header("Top Area Scroll View - Dice Types")]
        public Transform DiceTypeContainer;
        public RectTransform TopHeaderBarRect;
        public Button DropdownChevronButton;
        public List<DiceButtonUI> DiceButtons = new List<DiceButtonUI>();
        #endregion

        #region Inspector Fields - Left Panel
        [Header("Left Panel Elements")]
        public TMP_Dropdown SkillDropdown;
        public TextMeshProUGUI ModifierText;
        public Button MinusButton;
        public Button PlusButton;
        public TextMeshProUGUI DCText;
        public Toggle SingleDieToggle;
        public Toggle AdvantageToggle;
        public Button HistoryTabButton;
        #endregion

        #region Inspector Fields - Right Panel & Action
        [Header("Right Panel Elements")]
        public TextMeshProUGUI SelectedSkillNameText;
        public TextMeshProUGUI SkillDescriptionText;
        public TextMeshProUGUI TargetInfoText;

        [Header("Center Action Elements")]
        public Button RollButton;
        public CanvasGroup ViewCanvasGroup;
        #endregion

        #region Private State
        private DiceType _currentSelectedType = DiceType.D20;
        private bool _topBarExpanded = true;
        private bool _isInitialized = false;
        #endregion

        #region Initialization (Single Source of Truth)
        /// <summary>
        /// Explicit initialization called by UIController master sequence.
        /// Prevents Awake/Start race conditions.
        /// </summary>
        public void InitializeView()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            Debug.Log("[SkillCheckView] Initializing view elements and button bindings...");

            SetupUIListeners();
            InitializeDiceButtons();
            ConfigureDropdownStyling();
            ConfigureTextFormatting();
        }

        private void Awake()
        {
            // Safety fallback if scene is run standalone without UIController
            if (!_isInitialized)
            {
                InitializeView();
            }
        }

        private void SetupUIListeners()
        {
            if (SkillDropdown != null)
            {
                SkillDropdown.onValueChanged.RemoveAllListeners();
                SkillDropdown.onValueChanged.AddListener((val) => OnSkillSelected?.Invoke(val));
            }
            if (MinusButton != null)
            {
                MinusButton.onClick.RemoveAllListeners();
                MinusButton.onClick.AddListener(() => {
                    AnimateButton(MinusButton.transform);
                    OnModifierAdjusted?.Invoke(-1);
                });
            }
            if (PlusButton != null)
            {
                PlusButton.onClick.RemoveAllListeners();
                PlusButton.onClick.AddListener(() => {
                    AnimateButton(PlusButton.transform);
                    OnModifierAdjusted?.Invoke(1);
                });
            }
            if (SingleDieToggle != null)
            {
                SingleDieToggle.onValueChanged.RemoveAllListeners();
                SingleDieToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnModeChanged?.Invoke(RollMode.SingleDie);
                });
            }
            if (AdvantageToggle != null)
            {
                AdvantageToggle.onValueChanged.RemoveAllListeners();
                AdvantageToggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnModeChanged?.Invoke(RollMode.AdvantageTwoDice);
                });
            }
            if (RollButton != null)
            {
                RollButton.onClick.RemoveAllListeners();
                RollButton.onClick.AddListener(() => {
                    AnimateButton(RollButton.transform);
                    OnRollClicked?.Invoke();
                });
            }
            if (DropdownChevronButton != null)
            {
                DropdownChevronButton.onClick.RemoveAllListeners();
                DropdownChevronButton.onClick.AddListener(ToggleTopBar);
            }
        }

        private void InitializeDiceButtons()
        {
            if (DiceButtons == null || DiceButtons.Count == 0)
            {
                DiceButtons = new List<DiceButtonUI>();
                if (DiceTypeContainer != null)
                {
                    DiceButtons.AddRange(DiceTypeContainer.GetComponentsInChildren<DiceButtonUI>(true));
                }
            }

            foreach (var b in DiceButtons)
            {
                if (b == null) continue;
                b.Initialize();
                b.OnClicked -= SelectDiceType;
                b.OnClicked += SelectDiceType;
            }

            Debug.Log($"[SkillCheckView] Initialized {DiceButtons.Count} dice buttons.");
        }
        #endregion

        #region Public API
        public void PopulateSkills(List<string> skillNames)
        {
            if (SkillDropdown == null) return;
            SkillDropdown.ClearOptions();
            SkillDropdown.AddOptions(skillNames);
        }

        public void UpdateSkillDisplay(string skillName, int dc, string description)
        {
            if (DCText != null) DCText.text = "Difficulty Class (DC " + dc.ToString() + ")";
            if (SelectedSkillNameText != null) SelectedSkillNameText.text = skillName;
            if (SkillDescriptionText != null) SkillDescriptionText.text = description;
            if (TargetInfoText != null) TargetInfoText.text = $"Target DC: {dc}";

            if (DCText != null) DCText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }

        public void UpdateModifierDisplay(int modifier)
        {
            if (ModifierText != null)
            {
                ModifierText.text = "Modifier (" + (modifier >= 0 ? "+" : "") + modifier.ToString() + ")";
                ModifierText.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (ViewCanvasGroup != null)
            {
                ViewCanvasGroup.interactable = interactable;
                ViewCanvasGroup.DOFade(interactable ? 1f : 0.4f, 0.3f);
            }
            if (RollButton != null)
            {
                RollButton.interactable = interactable;
            }
        }
        #endregion

        #region Dice Selection & Highlighting
        public void BindDiceTypeButtons(List<DiceButtonUI> buttons)
        {
            DiceButtons.Clear();
            foreach (var b in buttons)
            {
                if (b == null) continue;
                b.Initialize();
                b.OnClicked -= SelectDiceType;
                b.OnClicked += SelectDiceType;
                DiceButtons.Add(b);
            }

            HighlightSelectedDiceButton(_currentSelectedType);
        }

        public void SelectDiceType(DiceType type, DiceButtonUI targetButton)
        {
            SelectDiceTypeInternal(type, targetButton);
        }

        public void SelectDiceType(DiceType type)
        {
            SelectDiceTypeInternal(type, null);
        }

        private void SelectDiceTypeInternal(DiceType type, DiceButtonUI targetButton)
        {
            _currentSelectedType = type;
            Debug.Log($"[SkillCheckView] SelectDiceType: {type}, Button: {targetButton?.gameObject.name}");
            HighlightSelectedDiceButton(type, targetButton);
            OnDiceTypeSelected?.Invoke(type);
        }

        private void HighlightSelectedDiceButton(DiceType selectedType, DiceButtonUI targetButton = null)
        {
            if (targetButton == null)
            {
                // Find exact matching single button instance for selection (e.g. Btn_d20)
                foreach (var b in DiceButtons)
                {
                    if (b != null && b.gameObject.name.Equals("Btn_" + selectedType.ToString().ToLower(), StringComparison.OrdinalIgnoreCase))
                    {
                        targetButton = b;
                        break;
                    }
                }
                if (targetButton == null)
                {
                    foreach (var b in DiceButtons)
                    {
                        if (b != null && b.DiceType == selectedType)
                        {
                            targetButton = b;
                            break;
                        }
                    }
                }
            }

            foreach (var btn in DiceButtons)
            {
                if (btn != null)
                {
                    bool isTarget = (btn == targetButton);
                    btn.SetSelected(isTarget);
                }
            }
        }
        #endregion

        #region Top Bar Animation
        public void ToggleTopBar()
        {
            _topBarExpanded = !_topBarExpanded;
            Vector2 targetPos = _topBarExpanded ? new Vector2(0f, -30f) : new Vector2(0f, 65f);
            Debug.Log($"[SkillCheckView] ToggleTopBar expanded: {_topBarExpanded}, TargetPos: {targetPos}");

            if (TopHeaderBarRect != null)
            {
                TopHeaderBarRect.DOAnchorPos(targetPos, 0.4f, Ease.OutQuad);
            }
            if (DropdownChevronButton != null)
            {
                DropdownChevronButton.transform.DORotate(new Vector3(0f, 0f, _topBarExpanded ? 0f : 180f), 0.35f, Ease.OutQuad);
            }
        }
        #endregion

        #region Internal Formatting & Styling Helpers
        private void ConfigureDropdownStyling()
        {
            if (SkillDropdown == null || SkillDropdown.template == null) return;
            var template = SkillDropdown.template;
            
            var templateImg = template.GetComponent<Image>();
            if (templateImg != null)
            {
                templateImg.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
            }

            var viewport = template.Find("Viewport");
            if (viewport != null)
            {
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg != null)
                {
                    vpImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
                }
            }

            var item = template.GetComponentInChildren<Toggle>(true);
            if (item != null)
            {
                var colors = item.colors;
                colors.normalColor = new Color(0.18f, 0.18f, 0.24f, 1f);
                colors.highlightedColor = new Color(0.95f, 0.78f, 0.35f, 1f);
                colors.pressedColor = new Color(0.85f, 0.68f, 0.25f, 1f);
                colors.selectedColor = new Color(0.95f, 0.78f, 0.35f, 1f);
                item.colors = colors;

                var itemBg = item.transform.Find("Item Background")?.GetComponent<Image>();
                if (itemBg != null)
                {
                    itemBg.color = Color.white;
                }

                var itemLabel = item.transform.Find("Item Label")?.GetComponent<TextMeshProUGUI>();
                if (itemLabel != null)
                {
                    itemLabel.color = new Color(0.95f, 0.95f, 0.98f, 1f);
                }
            }
        }

        private void ConfigureTextFormatting()
        {
            if (ModifierText != null)
            {
                ModifierText.textWrappingMode = TextWrappingModes.NoWrap;
                ModifierText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (DCText != null)
            {
                DCText.textWrappingMode = TextWrappingModes.NoWrap;
                DCText.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (SelectedSkillNameText != null)
            {
                SelectedSkillNameText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void AnimateButton(Transform target)
        {
            if (target != null)
            {
                target.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.2f);
            }
        }
        #endregion
    }
}
