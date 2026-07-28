using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface IDiceService
    {
        event Action<Transform, float> OnDiceImpact;
        event Action<DiceType> OnDiceTypeChanged;

        DiceType CurrentDiceType { get; set; }
        bool IsRolling { get; }

        Task<List<int>> RollDiceAsync(RollMode mode);
        void SpawnPreviewDice(DiceType type);
        void ClearActiveDice();
    }
}
