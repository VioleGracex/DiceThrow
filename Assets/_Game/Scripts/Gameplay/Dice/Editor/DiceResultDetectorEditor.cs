#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BG3DiceSystem.Gameplay.Dice;

namespace BG3DiceSystem.Gameplay.Dice.Editor
{
    [CustomEditor(typeof(DiceResultDetector))]
    public class DiceResultDetectorEditor : UnityEditor.Editor
    {
        private bool _faceListFoldout = true;

        public override void OnInspectorGUI()
        {
            DiceResultDetector detector = (DiceResultDetector)target;
            serializedObject.Update();

            // ── Dice Configuration ────────────────────────────────────
            EditorGUILayout.LabelField("Dice Configuration", EditorStyles.boldLabel);
            SerializedProperty typeProp = serializedObject.FindProperty("Type");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(typeProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                detector.ResetToDefaults();
                serializedObject.Update();
            }

            EditorGUILayout.Space(6);

            // ── Face Euler Rotations list ─────────────────────────────
            _faceListFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                _faceListFoldout, $"Face Euler Rotations  ({detector.MaxFaces} faces)");

            if (_faceListFoldout)
            {
                EditorGUI.indentLevel++;

                // Column header
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Face #", EditorStyles.miniLabel, GUILayout.Width(46));
                GUILayout.Label("X (Pitch)", EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.Label("Y (Yaw)", EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.Label("Z (Roll)", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                SerializedProperty facesProp = serializedObject.FindProperty("FaceRotations");

                if (facesProp != null && facesProp.isArray)
                {
                    for (int i = 0; i < facesProp.arraySize; i++)
                    {
                        SerializedProperty entry       = facesProp.GetArrayElementAtIndex(i);
                        SerializedProperty faceValProp = entry.FindPropertyRelative("FaceValue");
                        SerializedProperty eulerProp   = entry.FindPropertyRelative("EulerRotation");

                        bool isSelected = (faceValProp.intValue == detector.TestFaceValue);

                        // Highlight selected row
                        if (isSelected)
                        {
                            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(18));
                            EditorGUI.DrawRect(rect, new Color(1f, 0.8f, 0f, 0.15f));
                        }
                        else
                        {
                            EditorGUILayout.BeginHorizontal();
                        }

                        GUILayout.Label($"Face {faceValProp.intValue}", GUILayout.Width(46));

                        Vector3 euler = eulerProp.vector3Value;
                        EditorGUI.BeginChangeCheck();

                        float x = EditorGUILayout.FloatField(euler.x, GUILayout.Width(70));
                        float y = EditorGUILayout.FloatField(euler.y, GUILayout.Width(70));
                        float z = EditorGUILayout.FloatField(euler.z, GUILayout.Width(70));

                        if (EditorGUI.EndChangeCheck())
                        {
                            eulerProp.vector3Value = new Vector3(x, y, z);
                        }

                        // Quick "Apply" button — orients die to this face immediately
                        if (GUILayout.Button("▶", GUILayout.Width(22), GUILayout.Height(16)))
                        {
                            Undo.RecordObject(detector, "Orient To Face");
                            Undo.RecordObject(detector.transform, "Orient To Face");
                            detector.TestFaceValue = faceValProp.intValue;
                            detector.OrientToTestFace();
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);

                if (GUILayout.Button("Reset To Defaults", GUILayout.Height(22)))
                {
                    Undo.RecordObject(detector, "Reset Dice Face Rotations");
                    detector.ResetToDefaults();
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // ── Test & Inspection Tools ───────────────────────────────
            EditorGUILayout.LabelField("Test & Inspection Tools", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GizmoSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NormalFaceColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SelectedFaceColor"));

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            int newTestVal = EditorGUILayout.IntSlider("Test Face Value", detector.TestFaceValue, 1, detector.MaxFaces);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(detector, "Change Test Face Value");
                detector.TestFaceValue = newTestVal;
                EditorUtility.SetDirty(detector);
            }

            EditorGUILayout.Space(4);

            if (GUILayout.Button($"Orient To Test Face  ({detector.TestFaceValue})", GUILayout.Height(30)))
            {
                Undo.RecordObject(detector.transform, "Orient To Test Face");
                detector.OrientToTestFace();
            }

            if (GUILayout.Button("Detect Upward Face", GUILayout.Height(25)))
            {
                detector.TestDetectUpwardFace();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
