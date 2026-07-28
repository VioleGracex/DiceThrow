using UnityEngine;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface IAudioService
    {
        void PlayButtonClick();
        void PlayDiceThrow();
        void PlayDiceBounce();
        void PlayHeavyLanding();
        void PlaySuccess();
        void PlayFailure();
        void PlayCriticalSuccess();
        void PlayCriticalFailure();
    }
}
