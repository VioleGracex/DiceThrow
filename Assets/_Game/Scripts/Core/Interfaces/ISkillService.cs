using System;
using System.Collections.Generic;
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
        int CurrentDC { get; }

        void SelectSkill(int index);
        void SetModifier(int value);
        void AdjustModifier(int delta);
    }
}
