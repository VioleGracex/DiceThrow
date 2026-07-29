using System;
using System.Collections.Generic;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Skills;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface ISkillService
    {
        event Action OnSkillChanged;
        event Action OnModifierChanged;

        IReadOnlyList<SkillCheckSO> AvailableSkills { get; }
        SkillCheckSO CurrentSkill { get; }
        int CurrentModifier { get; }
        int BaseModifier { get; }
        int CurrentDC { get; }
        DiceType CurrentDiceType { get; }
        IReadOnlyList<ModifierData> ActiveModifiers { get; }
        int MaxModifierCards { get; }

        void SelectSkill(int index);
        void SetModifier(int value);
        void AdjustModifier(int delta);
        void SetDiceType(DiceType diceType);
        bool AddModifier(string name, int value);
        bool AdjustModifierValue(string id, int delta);
        bool RemoveModifier(string id);
        void ClearModifiers();
    }
}
