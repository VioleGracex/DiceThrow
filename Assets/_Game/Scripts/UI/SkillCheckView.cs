using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;

namespace BG3DiceSystem.UI
{
    public class SkillCheckView : MonoBehaviour
    {
        private ILocalizationService _localizationService;
        #region Events
        public event Action<int> OnSkillSelected;
        public event Action<int> OnModifierAdjusted;
        public event Action<RollMode> OnModeChanged;
        public event Action<DiceType> OnDiceTypeSelected;
        public event Action OnRollClicked;
        public event Action<string, int> OnAddModifierRequested;
        public event Action<string, int> OnAdjustModifierValueRequested;
        public event Action<string> OnRemoveModifierRequested;
        public event Action OnAutoTestClicked;
        #endregion

        #region Inspector Fields - Top Area & Dice Buttons
        [Header("Top Area Scroll View - Dice Types")]
        public Transform DiceTypeContainer;
        public RectTransform TopHeaderBarRect;
        public Button DropdownChevronButton;
        public List<DiceButtonUI> DiceButtons = new List<DiceButtonUI>();

        [Header("Top Area Difficulty Class Banner")]
        public GameObject TopDCBannerContainer;
        public TextMeshProUGUI TopDCHeaderLabelText;
        public TextMeshProUGUI TopDCNumberValueText;
        #endregion

        #region Inspector Fields - Left Panel
        [Header("Left Panel Elements")]
        public RectTransform LeftPanelRect;
        public TMP_Dropdown SkillDropdown;
        public TextMeshProUGUI ModifierText;
        public Button MinusButton;
        public Button PlusButton;
        public TextMeshProUGUI DCText;
        public Toggle SingleDieToggle;
        public Toggle AdvantageToggle;
        public Button HistoryTabButton;
        public Button AutoTestButton;

        [Header("Left Panel ScrollView & Modifier Cards")]
        public ScrollRect ModifierScrollRect;
        public Transform ModifierCardsContainer;
        public TextMeshProUGUI ModifierCountText;
        public Button AddModifierButton;
        public Button PresetGuidanceButton;
        public Button PresetProficiencyButton;
        public Button PresetPlusOneButton;
        public ModifierCardUI ModifierCardPrefab;
        #endregion

        #region Inspector Fields - Right Panel & Action
        [Header("Right Panel Elements")]
        public RectTransform RightPanelRect;
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

            EnsurePanelLayouts();
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
                    UpdateToggleVisuals();
                    if (isOn) OnModeChanged?.Invoke(RollMode.SingleDie);
                });
            }
            if (AdvantageToggle != null)
            {
                AdvantageToggle.onValueChanged.RemoveAllListeners();
                AdvantageToggle.onValueChanged.AddListener((isOn) => {
                    UpdateToggleVisuals();
                    if (isOn) OnModeChanged?.Invoke(RollMode.AdvantageTwoDice);
                });
            }
            UpdateToggleVisuals();
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

            // Preset & Add Modifier Button Listeners
            if (PresetGuidanceButton != null)
            {
                PresetGuidanceButton.onClick.RemoveAllListeners();
                PresetGuidanceButton.onClick.AddListener(() => {
                    AnimateButton(PresetGuidanceButton.transform);
                    OnAddModifierRequested?.Invoke("Guidance", 2);
                });
            }
            if (PresetProficiencyButton != null)
            {
                PresetProficiencyButton.onClick.RemoveAllListeners();
                PresetProficiencyButton.onClick.AddListener(() => {
                    AnimateButton(PresetProficiencyButton.transform);
                    OnAddModifierRequested?.Invoke("Proficiency", 2);
                });
            }
            if (PresetPlusOneButton != null)
            {
                PresetPlusOneButton.onClick.RemoveAllListeners();
                PresetPlusOneButton.onClick.AddListener(() => {
                    AnimateButton(PresetPlusOneButton.transform);
                    OnAddModifierRequested?.Invoke("Bonus", 1);
                });
            }
            if (AddModifierButton != null)
            {
                AddModifierButton.onClick.RemoveAllListeners();
                AddModifierButton.onClick.AddListener(() => {
                    AnimateButton(AddModifierButton.transform);
                    OnAddModifierRequested?.Invoke("Bonus", 1);
                });
            }
            if (AutoTestButton != null)
            {
                AutoTestButton.onClick.RemoveAllListeners();
                AutoTestButton.onClick.AddListener(() => {
                    AnimateButton(AutoTestButton.transform);
                    OnAutoTestClicked?.Invoke();
                });
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
        private IReadOnlyList<ModifierData> _lastActiveModifiers;
        private int _lastMaxLimit = 5;

        public void SetLocalizationService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            RefreshLocalization();
        }

        public void RefreshLocalization()
        {
            if (TopDCHeaderLabelText != null)
            {
                TopDCHeaderLabelText.text = _localizationService != null ? _localizationService.GetText("dc_banner_header") : "DIFFICULTY CLASS";
            }
            if (RollButton != null)
            {
                var label = RollButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("roll_button") : "ROLL";
            }
            if (PresetGuidanceButton != null)
            {
                var label = PresetGuidanceButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("preset_guidance") : "Guidance (+2)";
            }
            if (PresetProficiencyButton != null)
            {
                var label = PresetProficiencyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("preset_proficiency") : "Proficiency (+2)";
            }
            if (PresetPlusOneButton != null)
            {
                var label = PresetPlusOneButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("preset_plus_one") : "Bonus (+1)";
            }
            if (AddModifierButton != null)
            {
                var label = AddModifierButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("add_modifier") : "+ Add Modifier";
            }
            if (HistoryTabButton != null)
            {
                var label = HistoryTabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("tab_history") : "History";
            }
            if (AutoTestButton != null)
            {
                var label = AutoTestButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = _localizationService != null ? _localizationService.GetText("tab_autotests") : "Auto Tests";
            }

            // Static panel titles if present
            if (LeftPanelRect != null)
            {
                var titleText = LeftPanelRect.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                if (titleText != null) titleText.text = _localizationService != null ? _localizationService.GetText("extra_settings_title") : "Extra Settings";
            }
            if (RightPanelRect != null)
            {
                var titleText = RightPanelRect.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                if (titleText != null) titleText.text = _localizationService != null ? _localizationService.GetText("current_ability_check_title") : "Current Ability Check";
                var headerText = RightPanelRect.Find("SelectedSkillHeader")?.GetComponent<TextMeshProUGUI>();
                if (headerText != null) headerText.text = _localizationService != null ? _localizationService.GetText("selected_skill_header") : "Selected Skill";
            }

            UpdateToggleVisuals();

            if (_lastActiveModifiers != null)
            {
                RenderModifierCards(_lastActiveModifiers, _lastMaxLimit);
            }
        }

        public void PopulateSkills(List<string> skillNames)
        {
            if (SkillDropdown == null) return;
            SkillDropdown.ClearOptions();
            List<string> localizedNames = new List<string>();
            foreach (var s in skillNames)
            {
                localizedNames.Add(_localizationService != null ? _localizationService.GetSkillName(s) : s);
            }
            SkillDropdown.AddOptions(localizedNames);
        }

        public void UpdateSkillDisplay(string skillName, int dc, string description)
        {
            if (DCText != null)
            {
                DCText.transform.DOKill();
                DCText.transform.localScale = Vector3.one;
                DCText.text = _localizationService != null ? _localizationService.GetText("dc_class_fmt", dc) : ("Difficulty Class (DC " + dc.ToString() + ")");
            }
            string localizedName = _localizationService != null ? _localizationService.GetSkillName(skillName) : skillName;
            string localizedDesc = _localizationService != null ? _localizationService.GetSkillDescription(skillName) : description;
            if (string.IsNullOrEmpty(localizedDesc)) localizedDesc = description;

            if (SelectedSkillNameText != null) {
                SelectedSkillNameText.text = localizedName;
                SelectedSkillNameText.color = new Color(0.22f, 0.08f, 0.03f, 1f);
            }
            if (SkillDescriptionText != null) {
                SkillDescriptionText.text = localizedDesc;
                SkillDescriptionText.color = new Color(0.18f, 0.14f, 0.10f, 1f);
            }
            if (TargetInfoText != null) {
                TargetInfoText.text = _localizationService != null ? _localizationService.GetText("target_dc_fmt", dc) : $"Target DC: {dc}";
                TargetInfoText.color = new Color(0.22f, 0.08f, 0.03f, 1f);
            }

            if (TopDCHeaderLabelText != null)
            {
                TopDCHeaderLabelText.text = _localizationService != null ? _localizationService.GetText("dc_banner_header") : "DIFFICULTY CLASS";
            }

            if (TopDCNumberValueText != null)
            {
                TopDCNumberValueText.transform.DOKill();
                TopDCNumberValueText.transform.localScale = Vector3.one;
                TopDCNumberValueText.text = dc.ToString();
            }
        }

        public void UpdateModifierDisplay(int modifier)
        {
            if (ModifierText != null)
            {
                ModifierText.transform.DOKill();
                ModifierText.transform.localScale = Vector3.one;
                string modVal = (modifier > 0 ? "+" : "") + modifier;
                string modString = _localizationService != null ? _localizationService.GetText("bonus_fmt", modVal) : ("Bonus (" + modVal + ")");
                ModifierText.text = modString;
            }
        }

        #region Modifier Cards List & Limit Handling
        public void RenderModifierCards(IReadOnlyList<ModifierData> activeModifiers, int maxLimit = 5)
        {
            _lastActiveModifiers = activeModifiers;
            _lastMaxLimit = maxLimit;

            int currentCount = activeModifiers != null ? activeModifiers.Count : 0;

            if (ModifierCountText != null)
            {
                ModifierCountText.text = _localizationService != null 
                    ? _localizationService.GetText("modifiers_list_fmt", currentCount) 
                    : $"Modifiers ({currentCount})";
                ModifierCountText.color = new Color(0.95f, 0.78f, 0.35f, 1f);
            }

            if (AddModifierButton != null) AddModifierButton.interactable = true;
            if (PresetGuidanceButton != null) PresetGuidanceButton.interactable = true;
            if (PresetProficiencyButton != null) PresetProficiencyButton.interactable = true;
            if (PresetPlusOneButton != null) PresetPlusOneButton.interactable = true;

            if (ModifierCardsContainer == null) return;

            // Clear existing spawned card elements
            foreach (Transform child in ModifierCardsContainer)
            {
                Destroy(child.gameObject);
            }

            if (activeModifiers == null || activeModifiers.Count == 0) return;

            foreach (var mod in activeModifiers)
            {
                if (mod == null) continue;

                ModifierCardUI cardInstance = null;
                if (ModifierCardPrefab != null)
                {
                    cardInstance = Instantiate(ModifierCardPrefab, ModifierCardsContainer);
                }
                else
                {
                    cardInstance = CreateFallbackCardUI(ModifierCardsContainer);
                }

                if (cardInstance != null)
                {
                    cardInstance.Initialize(mod, _localizationService);
                    cardInstance.OnAdjustValueRequested += (data, delta) =>
                    {
                        OnAdjustModifierValueRequested?.Invoke(data.Id, delta);
                    };
                    cardInstance.OnDeleteRequested += (data) =>
                    {
                        OnRemoveModifierRequested?.Invoke(data.Id);
                    };
                }
            }

            // Rebuild layout immediately for clean scroll view sizing
            LayoutRebuilder.ForceRebuildLayoutImmediate(ModifierCardsContainer as RectTransform);
        }

        private ModifierCardUI CreateFallbackCardUI(Transform parent)
        {
            GameObject cardObj = new GameObject("Card_ModifierItem", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(ModifierCardUI));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 40f);

            Image bg = cardObj.GetComponent<Image>();
#if UNITY_EDITOR
            var rowBGSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/dice_throw__0000_Modifier_Row_BG.png");
            if (rowBGSprite != null) { bg.sprite = rowBGSprite; bg.color = Color.white; }
            else { bg.color = new Color(0.16f, 0.16f, 0.22f, 0.95f); }
#else
            bg.color = new Color(0.16f, 0.16f, 0.22f, 0.95f);
#endif

            Outline outline = cardObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Name Text (Left)
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0.42f, 1f);
            nameRect.offsetMin = new Vector2(24f, 2f);
            nameRect.offsetMax = new Vector2(0f, -2f);
            TextMeshProUGUI nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 13;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = new Color(0.12f, 0.08f, 0.04f, 1f);
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;

            // Minus Button (-)
            GameObject minusObj = new GameObject("MinusBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            minusObj.transform.SetParent(cardObj.transform, false);
            RectTransform minusRect = minusObj.GetComponent<RectTransform>();
            minusRect.anchorMin = new Vector2(0.43f, 0.5f);
            minusRect.anchorMax = new UnityEngine.Vector2(0.43f, 0.5f);
            minusRect.pivot = new Vector2(0f, 0.5f);
            minusRect.sizeDelta = new Vector2(24f, 24f);
            Image minusImg = minusObj.GetComponent<Image>();
#if UNITY_EDITOR
            var minusSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/dice_throw__0002_-_Btn.png");
            if (minusSprite != null) { minusImg.sprite = minusSprite; minusImg.color = Color.white; }
            else { minusImg.color = new Color(0.22f, 0.22f, 0.30f, 0.95f); }
#else
            minusImg.color = new Color(0.22f, 0.22f, 0.30f, 0.95f);
#endif
            Button minusBtn = minusObj.GetComponent<Button>();

            GameObject mLabelObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            mLabelObj.transform.SetParent(minusObj.transform, false);
            RectTransform mlr = mLabelObj.GetComponent<RectTransform>();
            mlr.anchorMin = Vector2.zero; mlr.anchorMax = Vector2.one; mlr.offsetMin = Vector2.zero; mlr.offsetMax = Vector2.zero;
            TextMeshProUGUI mTMP = mLabelObj.GetComponent<TextMeshProUGUI>();
            mTMP.text = "-"; mTMP.fontSize = 14; mTMP.fontStyle = FontStyles.Bold; mTMP.color = Color.white; mTMP.alignment = TextAlignmentOptions.Center;

            // Value Text
            GameObject valObj = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(cardObj.transform, false);
            RectTransform valRect = valObj.GetComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.54f, 0f);
            valRect.anchorMax = new Vector2(0.68f, 1f);
            valRect.offsetMin = Vector2.zero; valRect.offsetMax = Vector2.zero;
            TextMeshProUGUI valTMP = valObj.GetComponent<TextMeshProUGUI>();
            valTMP.fontSize = 14;
            valTMP.fontStyle = FontStyles.Bold;
            valTMP.color = new Color(0.12f, 0.08f, 0.04f, 1f);
            valTMP.alignment = TextAlignmentOptions.Center;

            // Plus Button (+)
            GameObject plusObj = new GameObject("PlusBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            plusObj.transform.SetParent(cardObj.transform, false);
            RectTransform plusRect = plusObj.GetComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(0.70f, 0.5f);
            plusRect.anchorMax = new Vector2(0.70f, 0.5f);
            plusRect.pivot = new Vector2(0f, 0.5f);
            plusRect.sizeDelta = new Vector2(24f, 24f);
            Image plusImg = plusObj.GetComponent<Image>();
#if UNITY_EDITOR
            var plusSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/dice_throw__0003_+_Btn.png");
            if (plusSprite != null) { plusImg.sprite = plusSprite; plusImg.color = Color.white; }
            else { plusImg.color = new Color(0.22f, 0.22f, 0.30f, 0.95f); }
#else
            plusImg.color = new Color(0.22f, 0.22f, 0.30f, 0.95f);
#endif
            Button plusBtn = plusObj.GetComponent<Button>();

            GameObject pLabelObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            pLabelObj.transform.SetParent(plusObj.transform, false);
            RectTransform plr = pLabelObj.GetComponent<RectTransform>();
            plr.anchorMin = Vector2.zero; plr.anchorMax = Vector2.one; plr.offsetMin = Vector2.zero; plr.offsetMax = Vector2.zero;
            TextMeshProUGUI pTMP = pLabelObj.GetComponent<TextMeshProUGUI>();
            pTMP.text = "+"; pTMP.fontSize = 14; pTMP.fontStyle = FontStyles.Bold; pTMP.color = Color.white; pTMP.alignment = TextAlignmentOptions.Center;

            // Delete 'X' Button CanvasGroup & Container
            GameObject btnObj = new GameObject("DeleteButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0.5f);
            btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(24f, 24f);
            btnRect.anchoredPosition = new Vector2(-26f, 0f);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.85f, 0.25f, 0.25f, 0.9f);
            Button btn = btnObj.GetComponent<Button>();
            CanvasGroup cg = btnObj.GetComponent<CanvasGroup>();

            // X Label inside button
            GameObject xTextObj = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform xRect = xTextObj.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero; xRect.anchorMax = Vector2.one; xRect.offsetMin = Vector2.zero; xRect.offsetMax = Vector2.zero;
            TextMeshProUGUI xTMP = xTextObj.GetComponent<TextMeshProUGUI>();
            xTMP.text = "X"; xTMP.fontSize = 13; xTMP.color = Color.white; xTMP.alignment = TextAlignmentOptions.Center;

#if UNITY_EDITOR
            var roleModelTMP = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/Fonts/RoleModel_TMP.asset");
            if (roleModelTMP != null)
            {
                nameTMP.font = roleModelTMP;
                valTMP.font = roleModelTMP;
                mTMP.font = roleModelTMP;
                pTMP.font = roleModelTMP;
                xTMP.font = roleModelTMP;
            }
#endif

            ModifierCardUI cardUI = cardObj.GetComponent<ModifierCardUI>();
            cardUI.NameText = nameTMP;
            cardUI.ValueText = valTMP;
            cardUI.MinusButton = minusBtn;
            cardUI.PlusButton = plusBtn;
            cardUI.DeleteButton = btn;
            cardUI.DeleteButtonCanvasGroup = cg;
            cardUI.CardBackground = bg;
            cardUI.CardOutline = outline;

            return cardUI;
        }
        #endregion

        #region Dynamic Layout Configuration for Panels
        public void EnsurePanelLayouts()
        {
            // Only ensure content layout for modifier card items container if missing
            if (ModifierCardsContainer != null)
            {
                var vert = ModifierCardsContainer.GetComponent<VerticalLayoutGroup>();
                if (vert == null) vert = ModifierCardsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                vert.spacing = 6f;
                vert.padding = new RectOffset(4, 4, 4, 4);
                vert.childControlWidth = true;
                vert.childControlHeight = false;
                vert.childForceExpandWidth = true;
                vert.childForceExpandHeight = false;

                var csf = ModifierCardsContainer.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = ModifierCardsContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        #endregion

        public void SetInteractable(bool interactable)
        {
            if (ViewCanvasGroup != null)
            {
                ViewCanvasGroup.DOKill();
                ViewCanvasGroup.interactable = interactable;
                ViewCanvasGroup.alpha = 1f;
            }
            if (RollButton != null)
            {
                RollButton.interactable = interactable;
            }
            if (DiceButtons != null)
            {
                foreach (var b in DiceButtons)
                {
                    if (b != null)
                    {
                        b.SetInteractable(interactable);
                    }
                }
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
                TopHeaderBarRect.DOKill();
                TopHeaderBarRect.DOAnchorPos(targetPos, 0.4f, Ease.OutQuad);
            }
            if (DropdownChevronButton != null)
            {
                DropdownChevronButton.transform.DOKill();
                DropdownChevronButton.transform.localRotation = Quaternion.identity;
            }
        }
        #endregion

        #region Internal Formatting & Styling Helpers
        private void ConfigureDropdownStyling()
        {
            if (SkillDropdown == null || SkillDropdown.template == null) return;
            var template = SkillDropdown.template;
            
            template.sizeDelta = new Vector2(0f, 180f);
            template.anchoredPosition = new Vector2(0f, -4f);

            var templateImg = template.GetComponent<Image>();
            if (templateImg != null)
            {
                templateImg.color = (templateImg.sprite != null) ? Color.white : new Color(0.16f, 0.12f, 0.08f, 0.98f);
            }

            var viewport = template.Find("Viewport");
            if (viewport != null)
            {
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg != null)
                {
                    vpImg.color = (vpImg.sprite != null) ? Color.white : new Color(0.16f, 0.12f, 0.08f, 0.98f);
                }

                var content = viewport.Find("Content");
                if (content != null)
                {
                    var vert = content.GetComponent<VerticalLayoutGroup>();
                    if (vert == null) vert = content.gameObject.AddComponent<VerticalLayoutGroup>();
                    vert.spacing = 2f;
                    vert.padding = new RectOffset(2, 2, 2, 2);
                    vert.childControlWidth = true;
                    vert.childControlHeight = false;
                    vert.childForceExpandWidth = true;
                    vert.childForceExpandHeight = false;

                    var csf = content.GetComponent<ContentSizeFitter>();
                    if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            var item = template.GetComponentInChildren<Toggle>(true);
            if (item != null)
            {
                var itemRect = item.transform as RectTransform;
                if (itemRect != null)
                {
                    itemRect.sizeDelta = new Vector2(0f, 36f);
                }

                var colors = item.colors;
                colors.normalColor = new Color(0.22f, 0.16f, 0.12f, 1f);
                colors.highlightedColor = new Color(0.78f, 0.58f, 0.22f, 1f);
                colors.pressedColor = new Color(0.65f, 0.48f, 0.18f, 1f);
                colors.selectedColor = new Color(0.78f, 0.58f, 0.22f, 1f);
                item.colors = colors;

                var itemBg = item.transform.Find("Item Background")?.GetComponent<Image>();
                if (itemBg != null)
                {
                    itemBg.color = Color.white;
                }

                var itemLabel = item.transform.Find("Item Label")?.GetComponent<TextMeshProUGUI>();
                if (itemLabel != null)
                {
                    var lRect = itemLabel.rectTransform;
                    lRect.offsetMin = new Vector2(14f, 0f);
                    lRect.offsetMax = new Vector2(-14f, 0f);
                    itemLabel.fontSize = 14;
                    itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
                    itemLabel.color = new Color(0.96f, 0.92f, 0.84f, 1f);
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
                target.DOKill();
                target.localScale = Vector3.one;
                target.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.2f);
            }
        }

        private void UpdateToggleVisuals()
        {
            if (SingleDieToggle != null)
            {
                bool isNormal = SingleDieToggle.isOn;
                var outline = SingleDieToggle.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = isNormal ? new Color(0.95f, 0.78f, 0.35f, 0.95f) : new Color(0.4f, 0.4f, 0.5f, 0.5f);
                }
                var label = SingleDieToggle.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.color = isNormal ? Color.white : new Color(0.7f, 0.7f, 0.75f, 1f);
                    label.text = _localizationService != null ? _localizationService.GetText("mode_single_die") : "Single Die";
                }
            }
            if (AdvantageToggle != null)
            {
                bool isAdv = AdvantageToggle.isOn;
                var outline = AdvantageToggle.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = isAdv ? new Color(0.95f, 0.78f, 0.35f, 0.95f) : new Color(0.4f, 0.4f, 0.5f, 0.5f);
                }
                var label = AdvantageToggle.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.color = isAdv ? Color.white : new Color(0.7f, 0.7f, 0.75f, 1f);
                    label.text = _localizationService != null ? _localizationService.GetText("mode_advantage") : "Advantage";
                }
            }
        }
        #endregion
    }
}
