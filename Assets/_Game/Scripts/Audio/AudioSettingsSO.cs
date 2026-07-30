using UnityEngine;

namespace BG3DiceSystem.Audio
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "BG3 Dice System/Audio Settings SO")]
    public class AudioSettingsSO : ScriptableObject
    {
        [Header("UI Sounds")]
        public AudioClip ButtonClick;
        public AudioClip DiceBtnClick;
        public AudioClip ScrollOpen;
        public AudioClip ChevronToggle;

        [Header("Card & Bonus Sounds")]
        public AudioClip CardSlide;
        public AudioClip BonusChime;

        [Header("Dice Physics Sounds")]
        public AudioClip DiceThrow;
        public AudioClip DiceBounce;
        public AudioClip HeavyLanding;
        public AudioClip DicePickup;

        [Header("Outcome Sounds")]
        public AudioClip Success;
        public AudioClip Failure;
        public AudioClip CriticalSuccess;
        public AudioClip CriticalFailure;
    }
}
