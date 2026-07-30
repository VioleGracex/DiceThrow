using UnityEngine;

namespace BG3DiceSystem.Gameplay.Roll
{
    [CreateAssetMenu(fileName = "RollSettings", menuName = "BG3 Dice System/Roll Settings SO")]
    public class RollSettingsSO : ScriptableObject
    {
        [Header("Aesthetic Colors")]
        public Color CriticalSuccessColor = new Color(0.95f, 0.82f, 0.35f, 1f); // Luminous Warm Gold
        public Color CriticalFailureColor = new Color(0.88f, 0.2f, 0.2f, 1f);  // Crimson Red
        public Color SuccessColor = new Color(0.35f, 0.88f, 0.45f, 1f);         // Luminous Medieval Emerald
        public Color FailureColor = new Color(0.88f, 0.35f, 0.30f, 1f);         // Warm Crimson / Red

        [Header("Animation Durations")]
        public float ResultAnimationDuration = 1.2f;
        public float DelayBeforeResultDisplay = 0.4f;
        public float ResultDisplayDurationSeconds = 3.5f;
    }
}
