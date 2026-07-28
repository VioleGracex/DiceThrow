using System;
using System.Collections.Generic;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Skills;

namespace BG3DiceSystem.Core.Services
{
    public class SkillService : ISkillService
    {
        public event Action OnSkillChanged;
        public event Action OnModifierChanged;

        private readonly List<SkillCheckSO> _availableSkills;
        private SkillCheckSO _currentSkill;
        private int _currentModifier;
        private DiceType _currentDiceType = DiceType.D20;

        public IReadOnlyList<SkillCheckSO> AvailableSkills => _availableSkills;
        public SkillCheckSO CurrentSkill => _currentSkill;
        public int CurrentModifier => _currentModifier;
        public DiceType CurrentDiceType => _currentDiceType;

        public int CurrentDC
        {
            get
            {
                if (_currentSkill == null) return 10;
                int baseDC = _currentSkill.DifficultyClass;
                int maxVal = GetMaxDieValue(_currentDiceType);
                // Scale DC proportionally from D20 base DC down to selected die type max value
                int scaledDC = Mathf.Clamp(Mathf.RoundToInt(baseDC * (maxVal / 20f)), 2, maxVal);
                return scaledDC;
            }
        }

        public SkillService(List<SkillCheckSO> skills)
        {
            _availableSkills = skills ?? new List<SkillCheckSO>();
            if (_availableSkills.Count > 0)
            {
                SelectSkill(0);
            }
        }

        public void SelectSkill(int index)
        {
            if (index < 0 || index >= _availableSkills.Count) return;
            _currentSkill = _availableSkills[index];
            _currentModifier = _currentSkill.DefaultModifier;
            OnSkillChanged?.Invoke();
            OnModifierChanged?.Invoke();
        }

        public void SetModifier(int value)
        {
            int clamped = Mathf.Clamp(value, -10, 10);
            if (_currentModifier != clamped)
            {
                _currentModifier = clamped;
                OnModifierChanged?.Invoke();
            }
        }

        public void AdjustModifier(int delta)
        {
            SetModifier(_currentModifier + delta);
        }

        public void SetDiceType(DiceType diceType)
        {
            if (_currentDiceType != diceType)
            {
                _currentDiceType = diceType;
                OnSkillChanged?.Invoke();
            }
        }

        public static int GetMaxDieValue(DiceType diceType)
        {
            switch (diceType)
            {
                case DiceType.D4: return 4;
                case DiceType.D6: return 6;
                case DiceType.D8: return 8;
                case DiceType.D10: return 10;
                case DiceType.D12: return 12;
                case DiceType.D20: default: return 20;
            }
        }
    }
}
