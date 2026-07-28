using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface IRollService
    {
        event Action OnRollStarted;
        event Action<FinalRoll> OnRollCompleted;

        RollMode CurrentRollMode { get; set; }
        IReadOnlyList<FinalRoll> RollHistory { get; }
        bool IsRolling { get; }

        Task<FinalRoll> ExecuteRollAsync();
        void ClearHistory();
    }
}
