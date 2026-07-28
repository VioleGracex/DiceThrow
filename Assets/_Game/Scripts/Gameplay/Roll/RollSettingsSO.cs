using UnityEngine;

namespace BG3DiceSystem.Gameplay.Roll
{
    [CreateAssetMenu(fileName = "RollSettings", menuName = "BG3 Dice System/Roll Settings SO")]
    public class RollSettingsSO : ScriptableObject
    {
        [Header("Aesthetic Colors")]
        public Color CriticalSuccessColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        public Color CriticalFailureColor = new Color(0.9f, 0.15f, 0.15f, 1f); // Crimson Red
        public Color SuccessColor = new Color(0.2f, 0.85f, 0.35f, 1f); // Emerald Green
        public Color FailureColor = new Color(0.85f, 0.3f, 0.2f, 1f); // Muted Dark Orange/Red

        [Header("Animation Durations")]
        public float ResultAnimationDuration = 1.2f;
        public float DelayBeforeResultDisplay = 0.4f;
        public float ResultDisplayDurationSeconds = 3.5f;
    }
}
