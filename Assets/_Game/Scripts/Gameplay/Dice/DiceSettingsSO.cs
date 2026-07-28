using UnityEngine;

namespace BG3DiceSystem.Gameplay.Dice
{
    [CreateAssetMenu(fileName = "DiceSettings", menuName = "BG3 Dice System/Dice Settings SO")]
    public class DiceSettingsSO : ScriptableObject
    {
        [Header("Throw Physics Parameters")]
        public float MinThrowForce = 7f;
        public float MaxThrowForce = 11f;
        public float MinTorque = 15f;
        public float MaxTorque = 30f;
        
        [Header("Spawn Settings")]
        public Vector3 SpawnPosition = new Vector3(0f, 3.5f, -0.5f);
        public Vector3 SpawnSpread = new Vector3(1.2f, 0.2f, 0f);

        [Header("Settling Criteria")]
        public float SleepVelocityThreshold = 0.05f;
        public float MaxRollTimeoutSeconds = 4.5f;
    }
}
