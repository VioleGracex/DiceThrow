using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;

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
        #endregion

        #region Private Fields & Dependencies
        private ISkillService _skillService;
        private IDiceService _diceService;
        private IRollService _rollService;
        private IAudioService _audioService;
        private bool _isInitialized;
        #endregion

        #region Dependency Injection
        [Inject]
        public void Construct(
            ISkillService skillService,
            IDiceService diceService,
            IRollService rollService,
            IAudioService audioService)
        {
            _skillService = skillService;
            _diceService = diceService;
            _rollService = rollService;
            _audioService = audioService;
        }
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

            // 1. Initialize Sub-views
            if (SkillCheckView != null)
            {
                SkillCheckView.InitializeView();
            }

            // 2. Populate Data Displays
            InitializeSkillView();

            // 3. Bind UI & Service Events
            SubscribeEvents();

            // 4. Set Initial State (Default D20 Dice Selection)
            if (_diceService != null)
            {
                Debug.Log("[UIController] Setting initial default dice selection to D20.");
                _diceService.CurrentDiceType = DiceType.D20;
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
        }

        private void UnsubscribeEvents()
        {
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
        }

        private void HandleDiceTypeSelected(DiceType type)
        {
            _audioService?.PlayButtonClick();
            ResultView?.HideResult();
            if (_diceService != null)
            {
                _diceService.CurrentDiceType = type;
            }
        }

        private async void HandleRollClicked()
        {
            if (_rollService == null || _rollService.IsRolling) return;

            ResultView?.HideResult();
            await _rollService.ExecuteRollAsync();
        }
        #endregion

        #region Service Event Handlers & Display Updates
        private void HandleRollStarted()
        {
            SkillCheckView?.SetInteractable(false);
        }

        private void HandleRollCompleted(FinalRoll result)
        {
            SkillCheckView?.SetInteractable(true);
            ResultView?.DisplayResult(result);
            HistoryView?.AddHistoryEntry(result);
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
        }
        #endregion
    }
}
