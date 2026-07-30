using UnityEngine;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface IAudioService
    {
        void PlayButtonClick();
        void PlayDiceBtnClick();
        void PlayScrollOpen();
        void PlayChevronToggle();
        void PlayCardSlide();
        void PlayBonusChime();
        void PlayDiceThrow();
        void PlayDiceBounce();
        void PlayHeavyLanding();
        void PlayDicePickup();
        void PlaySuccess();
        void PlayFailure();
        void PlayCriticalSuccess();
        void PlayCriticalFailure();
    }
}
