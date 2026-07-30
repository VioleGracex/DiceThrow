using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BG3DiceSystem.Gameplay.Dice
{
    [Serializable]
    public struct DiceFaceEntry
    {
        public int FaceValue;
        [Tooltip("Euler angles (X, Y, Z) of the die rotation that shows this face toward the camera.")]
        public Vector3 EulerRotation;
    }

    [ExecuteAlways]
    public class DiceResultDetector : MonoBehaviour
    {
        [Header("Dice Configuration")]
        public DiceType Type = DiceType.D20;

        [Header("Face Euler Rotations (edit per prefab)")]
        [Tooltip("Each entry = the die's Euler rotation that shows that face number toward the camera.")]
        public List<DiceFaceEntry> FaceRotations = new List<DiceFaceEntry>();

        [Header("Test & Inspection Tools")]
        public int TestFaceValue = 1;
        public bool ShowGizmos = true;
        public float GizmoSize = 0.08f;
        public Color NormalFaceColor = new Color(0f, 0.85f, 1f, 0.85f);
        public Color SelectedFaceColor = new Color(1f, 0.8f, 0f, 1f);

        #region Default Euler Rotation Tables (pre-baked for overlay camera -Z)
        private static List<DiceFaceEntry> GetDefaultRotations(DiceType type)
        {
            switch (type)
            {
                case DiceType.D4: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue = 1, EulerRotation = new Vector3(305.3f, 234.7f,  90.0f) },
                    new DiceFaceEntry { FaceValue = 2, EulerRotation = new Vector3(  0.0f,   0.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 3, EulerRotation = new Vector3( 54.7f, 234.7f, 270.0f) },
                    new DiceFaceEntry { FaceValue = 4, EulerRotation = new Vector3(  0.0f, 109.5f,   0.0f) },
                };

                case DiceType.D6: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue = 1, EulerRotation = new Vector3( 90.0f,   0.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 2, EulerRotation = new Vector3(  0.0f,   0.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 3, EulerRotation = new Vector3(  0.0f, 270.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 4, EulerRotation = new Vector3(  0.0f,  90.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 5, EulerRotation = new Vector3(  0.0f, 180.0f, 180.0f) },
                    new DiceFaceEntry { FaceValue = 6, EulerRotation = new Vector3(270.0f,   0.0f,   0.0f) },
                };

                case DiceType.D8: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue = 1, EulerRotation = new Vector3( 35.3f, 225.0f, 285.0f) },
                    new DiceFaceEntry { FaceValue = 2, EulerRotation = new Vector3( 35.3f,  45.0f,  15.0f) },
                    new DiceFaceEntry { FaceValue = 3, EulerRotation = new Vector3( 35.3f, 135.0f,  75.0f) },
                    new DiceFaceEntry { FaceValue = 4, EulerRotation = new Vector3( 35.3f, 315.0f, 345.0f) },
                    new DiceFaceEntry { FaceValue = 5, EulerRotation = new Vector3(324.7f, 135.0f, 285.0f) },
                    new DiceFaceEntry { FaceValue = 6, EulerRotation = new Vector3(324.7f, 315.0f,  15.0f) },
                    new DiceFaceEntry { FaceValue = 7, EulerRotation = new Vector3(324.7f, 225.0f,  75.0f) },
                    new DiceFaceEntry { FaceValue = 8, EulerRotation = new Vector3(324.7f,  45.0f, 345.0f) },
                };

                case DiceType.D10: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue =  1, EulerRotation = new Vector3(145.1f,  30.4f,  81.8f) },
                    new DiceFaceEntry { FaceValue =  2, EulerRotation = new Vector3( 45.0f,   0.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  3, EulerRotation = new Vector3( 34.9f, 149.6f,  98.4f) },
                    new DiceFaceEntry { FaceValue =  4, EulerRotation = new Vector3(325.1f, 329.5f,   9.8f) },
                    new DiceFaceEntry { FaceValue =  5, EulerRotation = new Vector3(315.1f, 180.0f, 180.0f) },
                    new DiceFaceEntry { FaceValue =  6, EulerRotation = new Vector3( 12.7f, 316.4f, 354.9f) },
                    new DiceFaceEntry { FaceValue =  7, EulerRotation = new Vector3(347.3f, 223.6f,  31.1f) },
                    new DiceFaceEntry { FaceValue =  8, EulerRotation = new Vector3( 12.6f,  43.6f,   5.0f) },
                    new DiceFaceEntry { FaceValue =  9, EulerRotation = new Vector3(347.4f, 136.4f, 329.2f) },
                    new DiceFaceEntry { FaceValue = 10, EulerRotation = new Vector3(325.1f,  30.4f, 350.2f) },
                };

                case DiceType.D12: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue =  1, EulerRotation = new Vector3( 31.7f, 270.0f, 328.3f) },
                    new DiceFaceEntry { FaceValue =  2, EulerRotation = new Vector3(  0.0f,  31.7f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  3, EulerRotation = new Vector3(328.3f,  90.0f, 328.3f) },
                    new DiceFaceEntry { FaceValue =  4, EulerRotation = new Vector3( 58.3f,   0.0f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  5, EulerRotation = new Vector3( 58.3f, 180.0f, 180.0f) },
                    new DiceFaceEntry { FaceValue =  6, EulerRotation = new Vector3(  0.0f, 211.7f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  7, EulerRotation = new Vector3(328.3f, 270.0f,  31.7f) },
                    new DiceFaceEntry { FaceValue =  8, EulerRotation = new Vector3(  0.0f, 328.3f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  9, EulerRotation = new Vector3(  0.0f, 148.3f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 10, EulerRotation = new Vector3( 31.7f,  90.0f,  31.7f) },
                    new DiceFaceEntry { FaceValue = 11, EulerRotation = new Vector3(301.7f, 180.0f, 180.0f) },
                    new DiceFaceEntry { FaceValue = 12, EulerRotation = new Vector3(301.7f,   0.0f,   0.0f) },
                };

                case DiceType.D20:
                default: return new List<DiceFaceEntry>
                {
                    new DiceFaceEntry { FaceValue =  1, EulerRotation = new Vector3(339.1f,  31.7f, 354.0f) },
                    new DiceFaceEntry { FaceValue =  2, EulerRotation = new Vector3( 35.3f, 166.7f, 139.8f) },
                    new DiceFaceEntry { FaceValue =  3, EulerRotation = new Vector3(  0.0f, 322.6f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  4, EulerRotation = new Vector3(324.7f, 166.7f, 220.2f) },
                    new DiceFaceEntry { FaceValue =  5, EulerRotation = new Vector3(  0.0f, 100.8f,   0.0f) },
                    new DiceFaceEntry { FaceValue =  6, EulerRotation = new Vector3(290.9f, 301.7f,  42.0f) },
                    new DiceFaceEntry { FaceValue =  7, EulerRotation = new Vector3( 20.9f,  31.7f,   6.0f) },
                    new DiceFaceEntry { FaceValue =  8, EulerRotation = new Vector3( 35.3f, 256.7f, 316.2f) },
                    new DiceFaceEntry { FaceValue =  9, EulerRotation = new Vector3(324.7f, 256.7f,  43.8f) },
                    new DiceFaceEntry { FaceValue = 10, EulerRotation = new Vector3( 69.1f, 301.7f, 318.0f) },
                    new DiceFaceEntry { FaceValue = 11, EulerRotation = new Vector3(290.9f, 121.7f, 258.0f) },
                    new DiceFaceEntry { FaceValue = 12, EulerRotation = new Vector3( 69.1f, 121.7f, 102.0f) },
                    new DiceFaceEntry { FaceValue = 13, EulerRotation = new Vector3(324.7f,  76.7f, 331.8f) },
                    new DiceFaceEntry { FaceValue = 14, EulerRotation = new Vector3(339.1f, 211.7f,  66.0f) },
                    new DiceFaceEntry { FaceValue = 15, EulerRotation = new Vector3( 35.3f,  76.7f,  28.2f) },
                    new DiceFaceEntry { FaceValue = 16, EulerRotation = new Vector3(  0.0f, 280.8f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 17, EulerRotation = new Vector3( 35.3f, 346.7f, 355.8f) },
                    new DiceFaceEntry { FaceValue = 18, EulerRotation = new Vector3(  0.0f, 142.6f,   0.0f) },
                    new DiceFaceEntry { FaceValue = 19, EulerRotation = new Vector3(324.7f, 346.7f,   4.2f) },
                    new DiceFaceEntry { FaceValue = 20, EulerRotation = new Vector3( 20.9f, 211.7f, 294.0f) },
                };
            }
        }
        #endregion

        public int MaxFaces => (FaceRotations != null && FaceRotations.Count > 0)
            ? FaceRotations.Count
            : GetDefaultRotations(Type).Count;

        private void Reset()   { ResetToDefaults(); }

        private void OnValidate()
        {
            if (FaceRotations == null || FaceRotations.Count == 0)
                ResetToDefaults();
            TestFaceValue = Mathf.Clamp(TestFaceValue, 1, MaxFaces);
        }

        [Button("Reset To Defaults")]
        public void ResetToDefaults()
        {
            FaceRotations = GetDefaultRotations(Type);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[DiceResultDetector] Reset {Type} to defaults ({FaceRotations.Count} faces).");
        }

        // ── Core API ────────────────────────────────────────────────────

        public Quaternion GetFacingRotation(int faceValue, Vector3 cameraDir)
        {
            if (FaceRotations == null || FaceRotations.Count == 0) ResetToDefaults();

            foreach (var entry in FaceRotations)
            {
                if (entry.FaceValue == faceValue)
                    return Quaternion.Euler(entry.EulerRotation);
            }
            return Quaternion.identity;
        }

        public int GetUpwardFaceValue()
        {
            if (FaceRotations == null || FaceRotations.Count == 0) ResetToDefaults();

            float minAngle = float.MaxValue;
            int bestValue = 1;

            foreach (var entry in FaceRotations)
            {
                float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(entry.EulerRotation));
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestValue = entry.FaceValue;
                }
            }

            return bestValue;
        }

        // ── Inspector Buttons ────────────────────────────────────────────

        [Button("Orient To Test Face")]
        public void OrientToTestFace()
        {
#if UNITY_EDITOR
            Undo.RecordObject(transform, "Orient To Test Face");
#endif
            transform.rotation = GetFacingRotation(TestFaceValue, -Vector3.forward);
            Debug.Log($"[DiceResultDetector] Oriented {Type} to show Face {TestFaceValue} facing camera.");
        }

        [Button("Detect Upward Face")]
        public void TestDetectUpwardFace()
        {
            int val = GetUpwardFaceValue();
            Debug.Log($"[DiceResultDetector] Currently detected face value: {val}");
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()        { DrawFaceGizmos(false); }
        private void OnDrawGizmosSelected() { DrawFaceGizmos(true);  }

        private void DrawFaceGizmos(bool selected)
        {
            if (!ShowGizmos || FaceRotations == null) return;

            bool isTriangularFace = (Type == DiceType.D4 || Type == DiceType.D8 || Type == DiceType.D20);

            foreach (var entry in FaceRotations)
            {
                bool isTestFace = (entry.FaceValue == TestFaceValue);
                Gizmos.color = isTestFace ? SelectedFaceColor : NormalFaceColor;

                // The face normal in world space = the rotation's forward, rotated by die's current rotation
                Quaternion storedRot = Quaternion.Euler(entry.EulerRotation);
                // Local face-forward: the -Z direction that stored rotation would point toward camera
                Vector3 localNorm = Quaternion.Inverse(storedRot) * (-Vector3.forward);
                Vector3 worldNorm = transform.rotation * localNorm;
                Vector3 pos = transform.position + worldNorm * 0.15f;

                float rayLength = isTestFace ? GizmoSize * 3f : GizmoSize * 1.8f;
                Gizmos.DrawLine(pos, pos + worldNorm * rayLength);
                Gizmos.DrawWireSphere(pos + worldNorm * rayLength, GizmoSize * 0.15f);

                Vector3 right = Vector3.Cross(worldNorm, Vector3.up);
                if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(worldNorm, Vector3.right);
                right.Normalize();
                Vector3 up = Vector3.Cross(worldNorm, right).normalized;
                float radius = isTestFace ? GizmoSize * 1.5f : GizmoSize;

                if (isTriangularFace)
                {
                    Gizmos.DrawLine(pos + up * radius, pos + (-up * 0.5f + right * 0.866f) * radius);
                    Gizmos.DrawLine(pos + (-up * 0.5f + right * 0.866f) * radius, pos + (-up * 0.5f - right * 0.866f) * radius);
                    Gizmos.DrawLine(pos + (-up * 0.5f - right * 0.866f) * radius, pos + up * radius);
                }
                else
                {
                    Gizmos.DrawLine(pos + ( right + up) * radius * 0.707f, pos + (-right + up) * radius * 0.707f);
                    Gizmos.DrawLine(pos + (-right + up) * radius * 0.707f, pos + (-right - up) * radius * 0.707f);
                    Gizmos.DrawLine(pos + (-right - up) * radius * 0.707f, pos + ( right - up) * radius * 0.707f);
                    Gizmos.DrawLine(pos + ( right - up) * radius * 0.707f, pos + ( right + up) * radius * 0.707f);
                }

#if UNITY_EDITOR
                GUIStyle style = new GUIStyle();
                style.normal.textColor = isTestFace ? Color.yellow : Color.white;
                style.fontSize = isTestFace ? 14 : 11;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                Handles.Label(pos + worldNorm * (rayLength + 0.02f), $"{entry.FaceValue}", style);
#endif
            }
        }
    }
}
