using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Services;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;

namespace BG3DiceSystem.Testing
{
    public class AutoPlayTestRunner : MonoBehaviour
    {
        #region Events
        public event Action OnTestSequenceStarted;
        public event Action<int, int, TestCaseResult> OnTestStepCompleted;
        public event Action<TestReport> OnTestSequenceCompleted;
        #endregion

        #region Inspector & Config Fields
        [Header("Test Suite Configuration")]
        [Tooltip("Pause duration in seconds between consecutive automated test rolls.")]
        public float WaitTimeBetweenTests = 2.0f;

        [Tooltip("If enabled, automatically launches test suite upon scene start.")]
        public bool AutoStartOnPlay = false;
        #endregion

        #region Private Dependencies & State
        private ISkillService _skillService;
        private IDiceService _diceService;
        private IRollService _rollService;

        private bool _isRunning;
        private CancellationTokenSource _cts;
        private TestReport _currentReport;
        #endregion

        #region Properties
        public bool IsRunning => _isRunning;
        public TestReport CurrentReport => _currentReport;
        #endregion

        #region Zenject Constructor / Dependency Injection
        [Inject]
        public void Construct(
            ISkillService skillService,
            IDiceService diceService,
            IRollService rollService)
        {
            _skillService = skillService;
            _diceService = diceService;
            _rollService = rollService;
        }
        #endregion

        private void Start()
        {
            if (AutoStartOnPlay && !_isRunning)
            {
                _ = RunAllTestsAsync();
            }
        }

        private void OnDestroy()
        {
            CancelTests();
        }

        public void CancelTests()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _isRunning = false;
        }

        #region Main Test Execution Pipeline
        public async Task<TestReport> RunAllTestsAsync()
        {
            if (_isRunning)
            {
                CancelTests();
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            if (_rollService == null || _diceService == null || _skillService == null)
            {
                Debug.LogError("[AutoPlayTestRunner] Cannot start test suite: Required gameplay services are not injected!");
                _isRunning = false;
                return null;
            }

            _currentReport = new TestReport("BG3 Dice System Automated Play Suite");
            float suiteStartTime = Time.time;

            OnTestSequenceStarted?.Invoke();
            Debug.Log("[AutoPlayTestRunner] === STARTING AUTOMATED GAMEPLAY TEST SUITE ===");

            // Define Test Cases
            var testCases = BuildTestSuite();
            int totalCount = testCases.Count;

            for (int i = 0; i < totalCount; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.LogWarning("[AutoPlayTestRunner] Automated test run was cancelled by user.");
                    break;
                }

                var tcDefinition = testCases[i];
                Debug.Log($"[AutoPlayTestRunner] Running Test [{i + 1}/{totalCount}]: {tcDefinition.TestName}");

                TestCaseResult result = await ExecuteSingleTestAsync(tcDefinition, token);
                _currentReport.Results.Add(result);

                OnTestStepCompleted?.Invoke(i + 1, totalCount, result);

                // Wait between tests so player/tester can observe animations and UI state
                if (i < totalCount - 1 && WaitTimeBetweenTests > 0f)
                {
                    int delayMs = Mathf.RoundToInt(WaitTimeBetweenTests * 1000f);
                    try
                    {
                        await Task.Delay(delayMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }

            _currentReport.TotalDurationSeconds = Time.time - suiteStartTime;
            _isRunning = false;

            Debug.Log($"[AutoPlayTestRunner] === TEST SUITE COMPLETE ===");
            Debug.Log(_currentReport.GenerateMarkdownReport());

            OnTestSequenceCompleted?.Invoke(_currentReport);
            return _currentReport;
        }
        #endregion

        #region Test Suite Definitions & Setup
        private struct TestCaseDefinition
        {
            public string TestName;
            public string Category;
            public DiceType DiceType;
            public RollMode RollMode;
            public int SkillIndex;
            public int BaseModifier;
            public List<ModifierData> CustomModifierCards;
        }

        private List<TestCaseDefinition> BuildTestSuite()
        {
            List<TestCaseDefinition> list = new List<TestCaseDefinition>();

            // 1. Single Die Throws across all Dice Types
            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D20 Standard",
                Category = "Single Die",
                DiceType = DiceType.D20,
                RollMode = RollMode.SingleDie,
                SkillIndex = 0,
                BaseModifier = 2,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D12 Heavy Throw",
                Category = "Single Die",
                DiceType = DiceType.D12,
                RollMode = RollMode.SingleDie,
                SkillIndex = 1,
                BaseModifier = 0,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D10 Standard Throw",
                Category = "Single Die",
                DiceType = DiceType.D10,
                RollMode = RollMode.SingleDie,
                SkillIndex = 2,
                BaseModifier = 1,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D8 Standard Throw",
                Category = "Single Die",
                DiceType = DiceType.D8,
                RollMode = RollMode.SingleDie,
                SkillIndex = 0,
                BaseModifier = 3,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D6 Standard Throw",
                Category = "Single Die",
                DiceType = DiceType.D6,
                RollMode = RollMode.SingleDie,
                SkillIndex = 1,
                BaseModifier = -1,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Single Die Roll - D4 Swift Throw",
                Category = "Single Die",
                DiceType = DiceType.D4,
                RollMode = RollMode.SingleDie,
                SkillIndex = 2,
                BaseModifier = 0,
                CustomModifierCards = null
            });

            // 2. Advantage 2-Dice Throws
            list.Add(new TestCaseDefinition
            {
                TestName = "Advantage 2-Dice Roll - D20",
                Category = "Advantage 2-Dice",
                DiceType = DiceType.D20,
                RollMode = RollMode.AdvantageTwoDice,
                SkillIndex = 0,
                BaseModifier = 2,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Advantage 2-Dice Roll - D6",
                Category = "Advantage 2-Dice",
                DiceType = DiceType.D6,
                RollMode = RollMode.AdvantageTwoDice,
                SkillIndex = 1,
                BaseModifier = 1,
                CustomModifierCards = null
            });

            // 3. Modifier Card System Tests
            list.Add(new TestCaseDefinition
            {
                TestName = "Modifier System - Guidance & Proficiency Stack",
                Category = "Modifiers",
                DiceType = DiceType.D20,
                RollMode = RollMode.SingleDie,
                SkillIndex = 0,
                BaseModifier = 0,
                CustomModifierCards = new List<ModifierData>
                {
                    new ModifierData("Guidance", 2, true),
                    new ModifierData("Proficiency", 3, true)
                }
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "Modifier System - Full 5 Cards Max Stack",
                Category = "Modifiers",
                DiceType = DiceType.D20,
                RollMode = RollMode.AdvantageTwoDice,
                SkillIndex = 2,
                BaseModifier = 1,
                CustomModifierCards = new List<ModifierData>
                {
                    new ModifierData("Athletics", 2, true),
                    new ModifierData("Wisdom", 1, true),
                    new ModifierData("Proficiency", 2, true),
                    new ModifierData("Guidance", 2, true),
                    new ModifierData("Bless", 1, true)
                }
            });

            // 4. DC Scaling & Skill Selection Test
            list.Add(new TestCaseDefinition
            {
                TestName = "Skill Selection & DC Scaling - D4 Scaled DC",
                Category = "DC Scaling",
                DiceType = DiceType.D4,
                RollMode = RollMode.SingleDie,
                SkillIndex = 3 < _skillService.AvailableSkills.Count ? 3 : 0,
                BaseModifier = 1,
                CustomModifierCards = null
            });

            list.Add(new TestCaseDefinition
            {
                TestName = "High DC Skill Check - D20 Advantage",
                Category = "DC Scaling",
                DiceType = DiceType.D20,
                RollMode = RollMode.AdvantageTwoDice,
                SkillIndex = 0,
                BaseModifier = 4,
                CustomModifierCards = new List<ModifierData>
                {
                    new ModifierData("Blessing", 2, true)
                }
            });

            return list;
        }
        #endregion

        #region Single Test Step Execution & Assertions
        private async Task<TestCaseResult> ExecuteSingleTestAsync(TestCaseDefinition def, CancellationToken token)
        {
            float stepStartTime = Time.time;
            TestCaseResult result = new TestCaseResult(def.TestName, def.Category)
            {
                DiceType = def.DiceType,
                RollMode = def.RollMode
            };

            // 1. Configure Services
            _diceService.CurrentDiceType = def.DiceType;
            _rollService.CurrentRollMode = def.RollMode;

            if (_skillService.AvailableSkills.Count > def.SkillIndex)
            {
                _skillService.SelectSkill(def.SkillIndex);
            }
            _skillService.SetModifier(def.BaseModifier);

            // Configure modifier cards
            if (def.CustomModifierCards != null)
            {
                _skillService.ClearModifiers();
                foreach (var mod in def.CustomModifierCards)
                {
                    _skillService.AddModifier(mod.Name, mod.Value);
                }
            }

            result.SkillName = _skillService.CurrentSkill != null ? _skillService.CurrentSkill.SkillName : "Skill Check";
            result.DifficultyClass = _skillService.CurrentDC;
            result.Modifier = _skillService.CurrentModifier;

            if (_skillService.ActiveModifiers != null)
            {
                foreach (var m in _skillService.ActiveModifiers)
                {
                    result.AppliedModifiers.Add($"{m.Name} (+{m.Value})");
                }
            }

            // 2. Execute Roll & await animations
            FinalRoll roll = default;
            try
            {
                roll = await _rollService.ExecuteRollAsync();
            }
            catch (Exception ex)
            {
                result.Fail($"Roll execution exception: {ex.Message}");
                result.DurationSeconds = Time.time - stepStartTime;
                return result;
            }

            // 3. Populate Results
            result.DiceValueA = roll.DiceValueA;
            result.DiceValueB = roll.DiceValueB;
            result.SelectedDiceValue = roll.SelectedDiceValue;
            result.Total = roll.Total;
            result.IsSuccess = roll.IsSuccess;
            result.IsCriticalSuccess = roll.IsCriticalSuccess;
            result.IsCriticalFailure = roll.IsCriticalFailure;

            result.OutcomeText = roll.IsCriticalSuccess ? "CRITICAL SUCCESS!" :
                                 (roll.IsCriticalFailure ? "CRITICAL FAILURE!" :
                                 (roll.IsSuccess ? "SUCCESS" : "FAILURE"));

            // 4. Assertions & Validation Rules

            // Rule A: Dice Value strictly within valid range [1, MaxDieValue]
            int maxDieVal = SkillService.GetMaxDieValue(def.DiceType);
            if (roll.SelectedDiceValue < 1 || roll.SelectedDiceValue > maxDieVal)
            {
                result.Fail($"Selected dice value {roll.SelectedDiceValue} is out of bounds for {def.DiceType} (expected 1-{maxDieVal}).");
            }
            if (roll.DiceValueA < 1 || roll.DiceValueA > maxDieVal)
            {
                result.Fail($"Die A value {roll.DiceValueA} is out of bounds for {def.DiceType}.");
            }

            // Rule B: Advantage Selection Rule
            if (def.RollMode == RollMode.AdvantageTwoDice)
            {
                int expectedMax = Mathf.Max(roll.DiceValueA, roll.DiceValueB);
                if (roll.SelectedDiceValue != expectedMax)
                {
                    result.Fail($"Advantage roll selected {roll.SelectedDiceValue}, but expected max of [{roll.DiceValueA}, {roll.DiceValueB}] = {expectedMax}.");
                }
            }

            // Rule C: Modifier Addition Math Check
            int expectedTotal = roll.SelectedDiceValue + roll.Modifier;
            if (roll.Total != expectedTotal)
            {
                result.Fail($"Total calculation error: SelectedValue ({roll.SelectedDiceValue}) + Modifier ({roll.Modifier}) = {expectedTotal}, but Total was {roll.Total}.");
            }

            // Rule D: Win / Loss Outcome Logic Verification
            if (roll.IsCriticalSuccess)
            {
                if (roll.SelectedDiceValue != maxDieVal)
                {
                    result.Fail($"Critical success flagged on non-max roll value {roll.SelectedDiceValue} (Max is {maxDieVal}).");
                }
                if (!roll.IsSuccess)
                {
                    result.Fail("Critical success must result in automatic success = true.");
                }
            }
            else if (roll.IsCriticalFailure)
            {
                if (roll.SelectedDiceValue != 1)
                {
                    result.Fail($"Critical failure flagged on roll value {roll.SelectedDiceValue} (expected 1).");
                }
                if (roll.IsSuccess)
                {
                    result.Fail("Critical failure must result in automatic success = false.");
                }
            }
            else
            {
                bool expectedSuccess = (roll.Total >= roll.DifficultyClass);
                if (roll.IsSuccess != expectedSuccess)
                {
                    result.Fail($"Outcome mismatch: Total {roll.Total} vs DC {roll.DifficultyClass}. Expected success={expectedSuccess}, got {roll.IsSuccess}.");
                }
            }

            result.DurationSeconds = Time.time - stepStartTime;
            return result;
        }
        #endregion
    }
}
