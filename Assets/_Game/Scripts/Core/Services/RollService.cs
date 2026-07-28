using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Core.Services
{
    public class RollService : IRollService
    {
        public event Action OnRollStarted;
        public event Action<FinalRoll> OnRollCompleted;

        private readonly IDiceService _diceService;
        private readonly ISkillService _skillService;
        private readonly IEffectsService _effectsService;
        private readonly IAudioService _audioService;
        private readonly List<FinalRoll> _history = new List<FinalRoll>();

        public RollMode CurrentRollMode { get; set; } = RollMode.SingleDie;
        public IReadOnlyList<FinalRoll> RollHistory => _history;
        public bool IsRolling => _diceService.IsRolling;

        public RollService(
            IDiceService diceService,
            ISkillService skillService,
            IEffectsService effectsService,
            IAudioService audioService)
        {
            _diceService = diceService;
            _skillService = skillService;
            _effectsService = effectsService;
            _audioService = audioService;

            if (_diceService != null)
            {
                _diceService.OnDiceImpact += HandleDiceImpact;
            }
        }

        private void HandleDiceImpact(Transform diceTransform, float force)
        {
            _audioService?.PlayDiceBounce();
            _effectsService?.PlayDiceImpact(diceTransform.position, force);
        }

        public async Task<FinalRoll> ExecuteRollAsync()
        {
            if (IsRolling) return default;

            OnRollStarted?.Invoke();
            _audioService?.PlayDiceThrow();

            List<int> diceValues = await _diceService.RollDiceAsync(CurrentRollMode);

            _audioService?.PlayHeavyLanding();
            _effectsService?.TriggerCameraShake(0.3f);

            int diceA = diceValues.Count > 0 ? diceValues[0] : 10;
            int diceB = diceValues.Count > 1 ? diceValues[1] : diceA;

            int selectedValue = (CurrentRollMode == RollMode.AdvantageTwoDice)
                ? Mathf.Max(diceA, diceB)
                : diceA;

            int modifier = _skillService.CurrentModifier;
            int dc = _skillService.CurrentDC;
            int total = selectedValue + modifier;

            int maxDieValue = SkillService.GetMaxDieValue(_diceService != null ? _diceService.CurrentDiceType : DiceType.D20);

            bool isNatMax = (selectedValue == maxDieValue);
            bool isNat1 = (selectedValue == 1);
            bool isSuccess = isNatMax || (!isNat1 && total >= dc);

            FinalRoll roll = new FinalRoll
            {
                SkillName = _skillService.CurrentSkill != null ? _skillService.CurrentSkill.SkillName : "Skill Check",
                Mode = CurrentRollMode,
                DiceValueA = diceA,
                DiceValueB = diceB,
                SelectedDiceValue = selectedValue,
                Modifier = modifier,
                Total = total,
                DifficultyClass = dc,
                IsSuccess = isSuccess,
                IsCriticalSuccess = isNatMax,
                IsCriticalFailure = isNat1
            };

            _history.Insert(0, roll);

            // Handle FX & SFX
            Vector3 overlayPos = new Vector3(1000f, 1000f, 0f);
            if (isNatMax)
            {
                _audioService?.PlayCriticalSuccess();
                _effectsService?.PlayCriticalSuccessExplosion(overlayPos);
            }
            else if (isNat1)
            {
                _audioService?.PlayCriticalFailure();
                _effectsService?.PlayFailureFlash();
            }
            else if (isSuccess)
            {
                _audioService?.PlaySuccess();
                _effectsService?.PlaySuccessGlow();
            }
            else
            {
                _audioService?.PlayFailure();
                _effectsService?.PlayFailureFlash();
            }

            await Task.Delay(400);

            OnRollCompleted?.Invoke(roll);
            return roll;
        }

        public void ClearHistory()
        {
            _history.Clear();
        }
    }
}
