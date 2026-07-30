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
        public const int MAX_MODIFIER_CARDS = 999;

        public event Action OnSkillChanged;
        public event Action OnModifierChanged;

        private readonly List<SkillCheckSO> _availableSkills;
        private readonly List<ModifierData> _activeModifiers = new List<ModifierData>();
        private SkillCheckSO _currentSkill;
        private int _baseModifier;
        private DiceType _currentDiceType = DiceType.D20;

        public IReadOnlyList<SkillCheckSO> AvailableSkills => _availableSkills;
        public SkillCheckSO CurrentSkill => _currentSkill;
        public int BaseModifier => _baseModifier;
        public DiceType CurrentDiceType => _currentDiceType;
        public IReadOnlyList<ModifierData> ActiveModifiers => _activeModifiers;
        public int MaxModifierCards => int.MaxValue;

        public int CurrentModifier
        {
            get
            {
                int total = _baseModifier;
                foreach (var mod in _activeModifiers)
                {
                    if (mod != null) total += mod.Value;
                }
                return total;
            }
        }

        public int CurrentDC
        {
            get
            {
                if (_currentSkill == null) return 10;
                int baseDC = _currentSkill.DifficultyClass;
                if (_currentDiceType == DiceType.D20)
                {
                    return baseDC;
                }
                int maxVal = GetMaxDieValue(_currentDiceType);
                // Scale DC proportionally down for smaller dice types
                int scaledDC = Mathf.Clamp(Mathf.RoundToInt(baseDC * (maxVal / 20f)), 2, maxVal);
                return scaledDC;
            }
        }

        public SkillService(List<SkillCheckSO> skills)
        {
            _availableSkills = skills ?? new List<SkillCheckSO>();
            InitializeDefaultModifiers();
            if (_availableSkills.Count > 0)
            {
                SelectSkill(0);
            }
        }

        private void InitializeDefaultModifiers()
        {
            _activeModifiers.Clear();
            _activeModifiers.Add(new ModifierData("Athletics", 0, true));
            _activeModifiers.Add(new ModifierData("Wisdom", 0, true));
            _activeModifiers.Add(new ModifierData("Proficiency", 0, true));
            _activeModifiers.Add(new ModifierData("Guidance", 0, true));
            _activeModifiers.Add(new ModifierData("Bless", 0, true));
        }

        public void SelectSkill(int index)
        {
            if (index < 0 || index >= _availableSkills.Count) return;
            _currentSkill = _availableSkills[index];
            _baseModifier = 0;
            OnSkillChanged?.Invoke();
            OnModifierChanged?.Invoke();
        }

        public void SetModifier(int value)
        {
            int clamped = Mathf.Clamp(value, -10, 10);
            if (_baseModifier != clamped)
            {
                _baseModifier = clamped;
                OnModifierChanged?.Invoke();
            }
        }

        public void AdjustModifier(int delta)
        {
            SetModifier(_baseModifier + delta);
        }

        public bool AddModifier(string name, int value)
        {
            var mod = new ModifierData(name, value, true);
            _activeModifiers.Add(mod);
            Debug.Log($"[SkillService] Added modifier '{name}' ({value}). Total active cards: {_activeModifiers.Count}");
            OnModifierChanged?.Invoke();
            return true;
        }

        public bool AdjustModifierValue(string id, int delta)
        {
            var mod = _activeModifiers.Find(m => m != null && m.Id == id);
            if (mod != null)
            {
                mod.Value = Mathf.Clamp(mod.Value + delta, -10, 10);
                Debug.Log($"[SkillService] Adjusted modifier '{mod.Name}' value to {mod.Value}");
                OnModifierChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool RemoveModifier(string id)
        {
            var mod = _activeModifiers.Find(m => m != null && m.Id == id);
            if (mod != null)
            {
                mod.Value = 0;
                Debug.Log($"[SkillService] Reset modifier '{mod.Name}' value to 0.");
                OnModifierChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void ClearModifiers()
        {
            if (_activeModifiers.Count > 0)
            {
                _activeModifiers.Clear();
                OnModifierChanged?.Invoke();
            }
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
