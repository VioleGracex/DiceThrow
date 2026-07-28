using System;
using System.Collections.Generic;
using UnityEngine;

namespace BG3DiceSystem.Gameplay.Dice
{
    [Serializable]
    public struct DiceFaceData
    {
        public int FaceValue;
        public Transform FaceTransform;
    }

    public class DiceResultDetector : MonoBehaviour
    {
        [Header("20 Face Normal Transforms")]
        public List<DiceFaceData> Faces = new List<DiceFaceData>();

        /// <summary>
        /// Calculates the upward-facing die value by finding the face transform 
        /// whose normal (transform.up) has the highest dot product with Vector3.up.
        /// </summary>
        public int GetUpwardFaceValue()
        {
            if (Faces == null || Faces.Count == 0)
            {
                Debug.LogWarning("[DiceResultDetector] No faces configured on die! Defaulting to 20.");
                return 20;
            }

            float maxDot = -2f;
            int bestValue = 1;

            foreach (var faceData in Faces)
            {
                if (faceData.FaceTransform == null) continue;

                // Dot product between face normal and world UP (0, 1, 0)
                float dot = Vector3.Dot(faceData.FaceTransform.up, Vector3.up);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestValue = faceData.FaceValue;
                }
            }

            return bestValue;
        }
    }
}
