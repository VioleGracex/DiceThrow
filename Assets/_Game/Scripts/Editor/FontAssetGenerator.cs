using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

namespace BG3DiceSystem.Editor
{
    public static class FontAssetGenerator
    {
        [MenuItem("Tools/BG3 System/Generate TMP Font Assets")]
        public static void GenerateFontAssets()
        {
            var cardivalFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/Cardival/CardivalDemoRegular-rvlvO.ttf");
            var roleModelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/RoleModel/RoleModelPersonalUseRegular-8MooA.otf");

            if (cardivalFont == null || roleModelFont == null)
            {
                Debug.LogError("[FontAssetGenerator] Failed to load source TTF/OTF fonts.");
                return;
            }

            var cAsset = CreateTMPFont(cardivalFont, "Cardival_TMP");
            var rAsset = CreateTMPFont(roleModelFont, "RoleModel_TMP");

            // Attach fallback font for Cyrillic / system character coverage
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset LiberationSans SDF");
            if (guids.Length > 0)
            {
                var defaultFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (defaultFallback != null)
                {
                    cAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { defaultFallback };
                    rAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { defaultFallback };
                    EditorUtility.SetDirty(cAsset);
                    EditorUtility.SetDirty(rAsset);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FontAssetGenerator] Created Cardival_TMP.asset and RoleModel_TMP.asset successfully!");
        }

        private static TMP_FontAsset CreateTMPFont(Font sourceFont, string assetName)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 
                72, 
                8, 
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 
                1024, 
                1024, 
                AtlasPopulationMode.Dynamic, 
                true
            );
            fontAsset.name = assetName;

            string path = "Assets/_Game/Art/Fonts/" + assetName + ".asset";
            AssetDatabase.CreateAsset(fontAsset, path);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D tex = fontAsset.atlasTextures[i];
                    if (tex != null)
                    {
                        tex.name = fontAsset.name + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        [MenuItem("Tools/BG3 System/Apply Fonts To Scene UI")]
        public static void ApplyFontsToSceneUI()
        {
            var cardivalTMP = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/Fonts/Cardival_TMP.asset");
            var roleModelTMP = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Art/Fonts/RoleModel_TMP.asset");

            if (cardivalTMP == null || roleModelTMP == null)
            {
                Debug.LogError("[FontAssetGenerator] TMP Font assets not found. Generate them first.");
                return;
            }

            var uiController = Object.FindObjectOfType<BG3DiceSystem.UI.UIController>();
            if (uiController == null)
            {
                Debug.LogError("[FontAssetGenerator] UIController missing in active scene.");
                return;
            }

            int cCount = 0;
            int rCount = 0;

            // 1. Cardival_TMP for Headers, Big Values, Action Roll Button, Status Badge
            if (uiController.SkillCheckView != null)
            {
                var scv = uiController.SkillCheckView;
                if (scv.TopDCHeaderLabelText != null) { scv.TopDCHeaderLabelText.font = cardivalTMP; EditorUtility.SetDirty(scv.TopDCHeaderLabelText); cCount++; }
                if (scv.TopDCNumberValueText != null) { scv.TopDCNumberValueText.font = cardivalTMP; EditorUtility.SetDirty(scv.TopDCNumberValueText); cCount++; }

                if (scv.LeftPanelRect != null)
                {
                    var t = scv.LeftPanelRect.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                    if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); cCount++; }
                }
                if (scv.RightPanelRect != null)
                {
                    var t = scv.RightPanelRect.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                    if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); cCount++; }
                }
                if (scv.RollButton != null)
                {
                    var t = scv.RollButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); cCount++; }
                }
            }

            if (uiController.ResultView != null)
            {
                var rv = uiController.ResultView;
                if (rv.TotalText != null) { rv.TotalText.font = cardivalTMP; EditorUtility.SetDirty(rv.TotalText); cCount++; }
                if (rv.StatusBadgeText != null) { rv.StatusBadgeText.font = cardivalTMP; EditorUtility.SetDirty(rv.StatusBadgeText); cCount++; }
            }

            if (uiController.AutoPlayTestView != null)
            {
                var aptv = uiController.AutoPlayTestView;
                if (aptv.TitleText != null) { aptv.TitleText.font = cardivalTMP; EditorUtility.SetDirty(aptv.TitleText); cCount++; }
            }

            // 2. RoleModel_TMP for Subheaders, Cards, Dropdowns, Labels, Toggles, Language Selector
            if (uiController.SkillCheckView != null)
            {
                var scv = uiController.SkillCheckView;
                if (scv.SelectedSkillNameText != null) { scv.SelectedSkillNameText.font = cardivalTMP; EditorUtility.SetDirty(scv.SelectedSkillNameText); rCount++; }
                if (scv.SkillDescriptionText != null) { scv.SkillDescriptionText.font = cardivalTMP; EditorUtility.SetDirty(scv.SkillDescriptionText); rCount++; }
                if (scv.TargetInfoText != null) { scv.TargetInfoText.font = cardivalTMP; EditorUtility.SetDirty(scv.TargetInfoText); rCount++; }
                if (scv.ModifierText != null) { scv.ModifierText.font = cardivalTMP; EditorUtility.SetDirty(scv.ModifierText); rCount++; }
                if (scv.ModifierCountText != null) { scv.ModifierCountText.font = cardivalTMP; EditorUtility.SetDirty(scv.ModifierCountText); rCount++; }
                if (scv.DCText != null) { scv.DCText.font = cardivalTMP; EditorUtility.SetDirty(scv.DCText); rCount++; }

                if (scv.RightPanelRect != null)
                {
                    var h = scv.RightPanelRect.Find("SelectedSkillHeader")?.GetComponent<TextMeshProUGUI>();
                    if (h != null) { h.font = cardivalTMP; EditorUtility.SetDirty(h); rCount++; }
                }

                if (scv.SingleDieToggle != null) { var t = scv.SingleDieToggle.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.AdvantageToggle != null) { var t = scv.AdvantageToggle.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }

                if (scv.PresetGuidanceButton != null) { var t = scv.PresetGuidanceButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.PresetProficiencyButton != null) { var t = scv.PresetProficiencyButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.PresetPlusOneButton != null) { var t = scv.PresetPlusOneButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.AddModifierButton != null) { var t = scv.AddModifierButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.HistoryTabButton != null) { var t = scv.HistoryTabButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }
                if (scv.AutoTestButton != null) { var t = scv.AutoTestButton.GetComponentInChildren<TextMeshProUGUI>(); if (t != null) { t.font = cardivalTMP; EditorUtility.SetDirty(t); } }

                foreach (var b in scv.DiceButtons)
                {
                    if (b != null && b.LabelText != null) { b.LabelText.font = cardivalTMP; EditorUtility.SetDirty(b.LabelText); }
                }
            }

            if (uiController.LanguageSelectorView != null)
            {
                var lsv = uiController.LanguageSelectorView;
                if (lsv.EnText != null) { lsv.EnText.font = cardivalTMP; EditorUtility.SetDirty(lsv.EnText); rCount++; }
                if (lsv.RuText != null) { lsv.RuText.font = cardivalTMP; EditorUtility.SetDirty(lsv.RuText); rCount++; }
            }

            if (uiController.SkillCheckView != null && uiController.SkillCheckView.ModifierCardPrefab != null)
            {
                var prefabCard = uiController.SkillCheckView.ModifierCardPrefab;
                if (prefabCard.NameText != null) { prefabCard.NameText.font = cardivalTMP; EditorUtility.SetDirty(prefabCard.NameText); }
                if (prefabCard.ValueText != null) { prefabCard.ValueText.font = cardivalTMP; EditorUtility.SetDirty(prefabCard.ValueText); }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[FontAssetGenerator] Applied Cardival_TMP font to all UI elements.");
        }
    }
}
