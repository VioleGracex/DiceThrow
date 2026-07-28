using System;
using System.Collections.Generic;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
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

        public IReadOnlyList<SkillCheckSO> AvailableSkills => _availableSkills;
        public SkillCheckSO CurrentSkill => _currentSkill;
        public int CurrentModifier => _currentModifier;
        public int CurrentDC => _currentSkill != null ? _currentSkill.DifficultyClass : 10;

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
    }
}
