using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;
using BG3DiceSystem.Testing;

namespace BG3DiceSystem.UI
{
    /// <summary>
    /// Master UI Controller acting as the single source of truth for UI initialization,
    /// view coordination, and event orchestration.
    /// </summary>
    public class UIController : MonoBehaviour, IInitializable
    {
        #region Inspector References
        [Header("Child Views")]
        public SkillCheckView SkillCheckView;
        public ResultView ResultView;
        public HistoryView HistoryView;
        public LanguageSelectorUI LanguageSelectorView;
        public QuitButtonUI QuitButtonView;

        [Header("Swipe & Click Touch Detector")]
        public DiceArenaSwipeDetector SwipeDetector;

        [Header("Automated Test Suite References")]
        public AutoPlayTestView AutoPlayTestView;
        public AutoPlayTestRunner AutoPlayTestRunner;
        #endregion

        #region Private Fields & Dependencies
        private ISkillService _skillService;
        private IDiceService _diceService;
        private IRollService _rollService;
        private IAudioService _audioService;
        private ILocalizationService _localizationService;
        private IEffectsService _effectsService;
        private bool _isInitialized;
        #endregion

        #region Dependency Injection
        [Inject]
        public void Construct(
            ISkillService skillService,
            IDiceService diceService,
            IRollService rollService,
            IAudioService audioService,
            ILocalizationService localizationService,
            [Inject(Optional = true)] IEffectsService effectsService = null)
        {
            _skillService = skillService;
            _diceService = diceService;
            _rollService = rollService;
            _audioService = audioService;
            _localizationService = localizationService;
            _effectsService = effectsService;
        }
        #endregion

        private bool _isSequenceAnimating;

        #region Public Properties
        public bool IsRolling => _isSequenceAnimating || (_rollService != null && _rollService.IsRolling) || (_diceService != null && _diceService.IsRolling);
        #endregion

        #region Unity Lifecycle & Initialization (Single Source of Truth)
        public void Initialize()
        {
            InitializeSystem();
        }

        private void Start()
        {
            // Fallback for non-Zenject scene startup or direct inspector testing
            if (!_isInitialized)
            {
                InitializeSystem();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// Single entry point for system initialization. Guarantees strict execution order
        /// to prevent lifecycle race conditions across sub-views and services.
        /// </summary>
        public void InitializeSystem()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            Debug.Log("[UIController] Starting Master Initialization Sequence...");

            // 1. Initialize Sub-views & Localization
            if (SkillCheckView != null)
            {
                SkillCheckView.SetLocalizationService(_localizationService);
                SkillCheckView.InitializeView();
            }

            if (ResultView != null)
            {
                ResultView.SetLocalizationService(_localizationService);
                ResultView.SetAudioService(_audioService);
                ResultView.SetEffectsService(_effectsService);
                ResultView.OnResultDisplayCompleted -= HandleResultDisplayCompleted;
                ResultView.OnResultDisplayCompleted += HandleResultDisplayCompleted;
                ResultView.HideResult();
            }

            if (HistoryView != null)
            {
                HistoryView.SetLocalizationService(_localizationService);
            }

            if (LanguageSelectorView == null)
            {
                LanguageSelectorView = GetComponentInChildren<LanguageSelectorUI>(true);
                if (LanguageSelectorView == null)
                {
                    GameObject langObj = new GameObject("LanguageSelectorUI");
                    langObj.transform.SetParent(transform, false);
                    LanguageSelectorView = langObj.AddComponent<LanguageSelectorUI>();
                }
            }

            if (LanguageSelectorView != null)
            {
                LanguageSelectorView.Initialize(_localizationService, _audioService);
            }

            if (QuitButtonView == null)
            {
                QuitButtonView = GetComponentInChildren<QuitButtonUI>(true);
                if (QuitButtonView == null)
                {
                    GameObject quitObj = new GameObject("QuitButtonUI");
                    quitObj.transform.SetParent(transform, false);
                    QuitButtonView = quitObj.AddComponent<QuitButtonUI>();
                }
            }

            if (QuitButtonView != null)
            {
                QuitButtonView.Initialize(_localizationService, _audioService);
            }

            if (AutoPlayTestRunner == null)
            {
                AutoPlayTestRunner = GetComponent<AutoPlayTestRunner>();
                if (AutoPlayTestRunner == null)
                {
                    AutoPlayTestRunner = gameObject.AddComponent<AutoPlayTestRunner>();
                }
            }

            if (AutoPlayTestRunner != null)
            {
                AutoPlayTestRunner.Construct(_skillService, _diceService, _rollService);
            }

            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.SetLocalizationService(_localizationService);
                AutoPlayTestView.InitializeView();
            }

            // 2. Populate Data Displays
            InitializeSkillView();

            // 3. Bind UI & Service Events
            SubscribeEvents();

            // 4. Set Initial State (Default D20 Dice Selection)
            if (_diceService != null)
            {
                Debug.Log("[UIController] Setting initial default dice selection to D20.");
                RollMode initMode = _rollService != null ? _rollService.CurrentRollMode : RollMode.SingleDie;
                _diceService.SpawnPreviewDice(DiceType.D20, initMode);
            }
            if (SkillCheckView != null)
            {
                SkillCheckView.SelectDiceType(DiceType.D20);
            }

            Debug.Log("[UIController] Master Initialization Complete.");
        }

        private void InitializeSkillView()
        {
            if (_skillService == null || SkillCheckView == null) return;

            List<string> skillNames = new List<string>();
            foreach (var skill in _skillService.AvailableSkills)
            {
                skillNames.Add(skill.SkillName);
            }
            SkillCheckView.PopulateSkills(skillNames);

            UpdateSkillDisplay();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeEvents()
        {
            if (SkillCheckView != null)
            {
                SkillCheckView.OnSkillSelected += HandleSkillSelected;
                SkillCheckView.OnModifierAdjusted += HandleModifierAdjusted;
                SkillCheckView.OnModeChanged += HandleModeChanged;
                SkillCheckView.OnDiceTypeSelected += HandleDiceTypeSelected;
                SkillCheckView.OnRollClicked += HandleRollClicked;
                SkillCheckView.OnAddModifierRequested += HandleAddModifierRequested;
                SkillCheckView.OnAdjustModifierValueRequested += HandleAdjustModifierValueRequested;
                SkillCheckView.OnRemoveModifierRequested += HandleRemoveModifierRequested;
                SkillCheckView.OnAutoTestClicked += HandleAutoTestClicked;
            }

            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.OnStartTestsRequested += HandleStartAutoTests;
                AutoPlayTestView.OnStopTestsRequested += HandleStopAutoTests;
                AutoPlayTestView.OnWaitTimeChanged += HandleTestWaitTimeChanged;
            }

            if (AutoPlayTestRunner != null)
            {
                AutoPlayTestRunner.OnTestSequenceStarted += HandleTestSequenceStarted;
                AutoPlayTestRunner.OnTestStepCompleted += HandleTestStepCompleted;
                AutoPlayTestRunner.OnTestSequenceCompleted += HandleTestSequenceCompleted;
            }

            if (_skillService != null)
            {
                _skillService.OnSkillChanged += UpdateSkillDisplay;
                _skillService.OnModifierChanged += UpdateModifierDisplay;
            }

            if (_rollService != null)
            {
                _rollService.OnRollStarted += HandleRollStarted;
                _rollService.OnRollCompleted += HandleRollCompleted;
            }

            if (_diceService != null)
            {
                _diceService.OnRollRequested += HandleRollClicked;
            }

            var directDetectors = UnityEngine.Object.FindObjectsByType<DiceDirectRaycastDetector>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            foreach (var d in directDetectors)
            {
                if (d != null)
                {
                    d.OnRollRequested -= HandleRollClicked;
                    d.OnRollRequested += HandleRollClicked;
                }
            }

            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= HandleLanguageChanged;
            }

            if (ResultView != null)
            {
                ResultView.OnResultDisplayCompleted -= HandleResultDisplayCompleted;
            }

            if (_diceService != null)
            {
                _diceService.OnRollRequested -= HandleRollClicked;
            }

            var directDetectors = UnityEngine.Object.FindObjectsByType<DiceDirectRaycastDetector>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            foreach (var d in directDetectors)
            {
                if (d != null)
                {
                    d.OnRollRequested -= HandleRollClicked;
                }
            }
            if (SkillCheckView != null)
            {
                SkillCheckView.OnSkillSelected -= HandleSkillSelected;
                SkillCheckView.OnModifierAdjusted -= HandleModifierAdjusted;
                SkillCheckView.OnModeChanged -= HandleModeChanged;
                SkillCheckView.OnDiceTypeSelected -= HandleDiceTypeSelected;
                SkillCheckView.OnRollClicked -= HandleRollClicked;
                SkillCheckView.OnAddModifierRequested -= HandleAddModifierRequested;
                SkillCheckView.OnAdjustModifierValueRequested -= HandleAdjustModifierValueRequested;
                SkillCheckView.OnRemoveModifierRequested -= HandleRemoveModifierRequested;
                SkillCheckView.OnAutoTestClicked -= HandleAutoTestClicked;
            }

            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.OnStartTestsRequested -= HandleStartAutoTests;
                AutoPlayTestView.OnStopTestsRequested -= HandleStopAutoTests;
                AutoPlayTestView.OnWaitTimeChanged -= HandleTestWaitTimeChanged;
            }

            if (AutoPlayTestRunner != null)
            {
                AutoPlayTestRunner.OnTestSequenceStarted -= HandleTestSequenceStarted;
                AutoPlayTestRunner.OnTestStepCompleted -= HandleTestStepCompleted;
                AutoPlayTestRunner.OnTestSequenceCompleted -= HandleTestSequenceCompleted;
            }

            if (_skillService != null)
            {
                _skillService.OnSkillChanged -= UpdateSkillDisplay;
                _skillService.OnModifierChanged -= UpdateModifierDisplay;
            }

            if (_rollService != null)
            {
                _rollService.OnRollStarted -= HandleRollStarted;
                _rollService.OnRollCompleted -= HandleRollCompleted;
            }
        }
        #endregion

        #region View Event Handlers
        private void HandleSkillSelected(int index)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            _skillService?.SelectSkill(index);
        }

        private void HandleModifierAdjusted(int delta)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            _skillService?.AdjustModifier(delta);
        }

        private void HandleAddModifierRequested(string name, int value)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            _skillService?.AddModifier(name, value);
        }

        private void HandleAdjustModifierValueRequested(string id, int delta)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            _skillService?.AdjustModifierValue(id, delta);
        }

        private void HandleRemoveModifierRequested(string id)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            _skillService?.RemoveModifier(id);
        }

        private void HandleModeChanged(RollMode mode)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            if (_rollService != null)
            {
                _rollService.CurrentRollMode = mode;
            }
            if (_diceService != null)
            {
                _diceService.SpawnPreviewDice(_diceService.CurrentDiceType, mode);
            }
        }

        private void HandleDiceTypeSelected(DiceType type)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            if (_diceService != null)
            {
                RollMode currentMode = _rollService != null ? _rollService.CurrentRollMode : RollMode.SingleDie;
                _diceService.SpawnPreviewDice(type, currentMode);
            }
        }

        private float _lastRollTime = -999f;
        public float RollCooldownSeconds = 5.0f;

        private async void HandleRollClicked()
        {
            if (Time.time - _lastRollTime < RollCooldownSeconds)
            {
                Debug.Log($"[UIController] Roll request ignored: 5s cooldown active ({RollCooldownSeconds - (Time.time - _lastRollTime):F1}s remaining).");
                return;
            }

            if (_rollService == null || _rollService.IsRolling || (_diceService != null && _diceService.IsRolling)) return;

            _lastRollTime = Time.time;

            // Trigger cooldown on raycast detectors immediately
            var directDetectors = UnityEngine.Object.FindObjectsByType<DiceDirectRaycastDetector>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            foreach (var d in directDetectors)
            {
                if (d != null) d.TriggerCooldown(RollCooldownSeconds);
            }

            ResultView?.HideResult();
            await _rollService.ExecuteRollAsync();
        }
        #endregion

        #region Service Event Handlers & Display Updates
        private void HandleRollStarted()
        {
            _isSequenceAnimating = true;
            SkillCheckView?.SetInteractable(false);
            SetDetectorsEnabled(false);
        }

        private void HandleRollCompleted(FinalRoll result)
        {
            if (result.SelectedDiceValue == 0 && result.Total == 0 && string.IsNullOrEmpty(result.SkillName))
            {
                Debug.LogWarning("[UIController] Ignored empty/default roll completed event.");
                _isSequenceAnimating = false;
                SkillCheckView?.SetInteractable(true);
                SetDetectorsEnabled(true);
                return;
            }

            ResultView?.DisplayResult(result);
            HistoryView?.AddHistoryEntry(result);
        }

        private void HandleResultDisplayCompleted()
        {
            _isSequenceAnimating = false;
            SkillCheckView?.SetInteractable(true);
            SetDetectorsEnabled(true);
            Debug.Log("[UIController] Roll result sequence animation completed. System ready for next roll.");
        }

        private void SetDetectorsEnabled(bool isEnabled)
        {
            var swipeDetectors = UnityEngine.Object.FindObjectsByType<DiceArenaSwipeDetector>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            foreach (var d in swipeDetectors)
            {
                if (d != null) d.IsEnabled = isEnabled;
            }

            var directDetectors = UnityEngine.Object.FindObjectsByType<DiceDirectRaycastDetector>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            foreach (var d in directDetectors)
            {
                if (d != null)
                {
                    d.IsEnabled = isEnabled;
                    if (!isEnabled)
                    {
                        d.TriggerCooldown(5.0f);
                    }
                }
            }
        }

        private void UpdateSkillDisplay()
        {
            if (_skillService?.CurrentSkill != null && SkillCheckView != null)
            {
                var skill = _skillService.CurrentSkill;
                SkillCheckView.UpdateSkillDisplay(skill.SkillName, skill.DifficultyClass, skill.Description);
                UpdateModifierDisplay();
            }
        }

        private void UpdateModifierDisplay()
        {
            if (_skillService != null && SkillCheckView != null)
            {
                SkillCheckView.UpdateModifierDisplay(_skillService.CurrentModifier);
                SkillCheckView.RenderModifierCards(_skillService.ActiveModifiers, _skillService.MaxModifierCards);
            }
            if (_skillService != null && ResultView != null)
            {
                ResultView.RefreshModifierCards(_skillService.ActiveModifiers, _skillService.BaseModifier);
            }
        }

        private void HandleLanguageChanged()
        {
            InitializeSkillView();
            SkillCheckView?.RefreshLocalization();
            ResultView?.RefreshLocalization();
            HistoryView?.RefreshLocalization();
            AutoPlayTestView?.RefreshLocalization();
        }
        #endregion

        #region Automated Test Handlers
        private void HandleAutoTestClicked()
        {
            _audioService?.PlayButtonClick();
            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.ShowView();
            }
            else
            {
                HandleStartAutoTests();
            }
        }

        private async void HandleStartAutoTests()
        {
            _audioService?.PlayButtonClick();
            if (AutoPlayTestRunner != null)
            {
                if (AutoPlayTestView != null && AutoPlayTestView.WaitTimeSlider != null)
                {
                    AutoPlayTestRunner.WaitTimeBetweenTests = AutoPlayTestView.WaitTimeSlider.value;
                }
                await AutoPlayTestRunner.RunAllTestsAsync();
            }
        }

        private void HandleStopAutoTests()
        {
            _audioService?.PlayButtonClick();
            if (AutoPlayTestRunner != null)
            {
                AutoPlayTestRunner.CancelTests();
            }
        }

        private void HandleTestWaitTimeChanged(float seconds)
        {
            if (AutoPlayTestRunner != null)
            {
                AutoPlayTestRunner.WaitTimeBetweenTests = seconds;
            }
        }

        private void HandleTestSequenceStarted()
        {
            SkillCheckView?.SetInteractable(false);
            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.SetRunningState(true);
            }
        }

        private void HandleTestStepCompleted(int current, int total, TestCaseResult result)
        {
            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.UpdateProgress(current, total, result);
            }
        }

        private void HandleTestSequenceCompleted(TestReport report)
        {
            SkillCheckView?.SetInteractable(true);
            if (AutoPlayTestView != null)
            {
                AutoPlayTestView.DisplayFinalReport(report);
            }
        }
        #endregion
    }
}
