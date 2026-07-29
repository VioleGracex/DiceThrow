using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.UI
{
    public class ResultView : MonoBehaviour
    {
        [Header("Containers & Canvas")]
        public CanvasGroup ViewCanvasGroup;

        [Header("Value Texts")]
        public TextMeshProUGUI TakenText;
        public TextMeshProUGUI TotalText;
        public TextMeshProUGUI StatusBadgeText;
        public Image StatusBadgeBackground;

        [Header("Modifier Cards Row")]
        public GameObject ModifierCardsRow;
        public Transform ResultCardsContainer;
        public ScrollRect ResultCardsScrollRect;

        [Header("Settings Reference")]
        public RollSettingsSO Settings;

        [Header("Display Duration Settings")]
        public float DisplayDurationSeconds = 3.5f;

        private Coroutine _resultSequenceCoroutine;

        private void Awake()
        {
            if (ViewCanvasGroup != null) ViewCanvasGroup.alpha = 0f;
        }

        public void DisplayResult(FinalRoll roll)
        {
            if (ViewCanvasGroup == null) return;

            // Log detailed roll result to console
            Debug.Log($"[RollResult] Raw Dice: {roll.SelectedDiceValue} (DieA={roll.DiceValueA}, DieB={roll.DiceValueB}), Modifier: +{roll.Modifier}, DC: {roll.DifficultyClass}, Total: {roll.Total}, Outcome: {(roll.IsCriticalSuccess ? "CRITICAL SUCCESS" : (roll.IsCriticalFailure ? "CRITICAL FAILURE" : (roll.IsSuccess ? "SUCCESS" : "FAILURE")))}");

            if (_resultSequenceCoroutine != null)
            {
                StopCoroutine(_resultSequenceCoroutine);
                _resultSequenceCoroutine = null;
            }

            _resultSequenceCoroutine = StartCoroutine(AnimateBG3ResultSequence(roll));
        }

        private System.Collections.IEnumerator AnimateBG3ResultSequence(FinalRoll roll)
        {
            ViewCanvasGroup.alpha = 1f;

            // Hide status badge initially for sequential reveal
            if (StatusBadgeText != null) StatusBadgeText.gameObject.SetActive(false);
            if (StatusBadgeBackground != null) StatusBadgeBackground.gameObject.SetActive(false);

            // 1. Show initial raw die result right under the die
            int rawVal = roll.SelectedDiceValue;
            if (TotalText != null)
            {
                TotalText.text = rawVal.ToString();
                TotalText.gameObject.SetActive(true);
                TotalText.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f);
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Populate & reveal modifier cards row
            UpdateModifierCards(roll);
            if (ModifierCardsRow != null)
            {
                ModifierCardsRow.SetActive(true);
                ModifierCardsRow.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f);
            }

            yield return new WaitForSeconds(0.35f);

            // 3. Animate value counting up from raw roll to final total
            if (roll.Modifier != 0)
            {
                int startVal = rawVal;
                int endVal = roll.Total;
                float duration = 0.4f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    int currentVal = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, elapsed / duration));
                    if (TotalText != null) TotalText.text = currentVal.ToString();
                    yield return null;
                }
                if (TotalText != null)
                {
                    TotalText.text = endVal.ToString();
                    TotalText.transform.DOPunchScale(Vector3.one * 0.35f, 0.35f);
                }
            }

            yield return new WaitForSeconds(0.3f);

            // 4. Reveal Final Outcome Status Badge at bottom
            Color badgeColor = GetBadgeColor(roll);
            string badgeText = roll.IsCriticalSuccess ? "CRITICAL SUCCESS!" :
                               (roll.IsCriticalFailure ? "CRITICAL FAILURE!" :
                               (roll.IsSuccess ? "SUCCESS" : "FAILURE"));

            if (StatusBadgeText != null)
            {
                StatusBadgeText.text = badgeText;
                StatusBadgeText.color = badgeColor;
                StatusBadgeText.gameObject.SetActive(true);
            }

            if (StatusBadgeBackground != null)
            {
                StatusBadgeBackground.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.35f);
                StatusBadgeBackground.gameObject.SetActive(true);
                StatusBadgeBackground.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f);
            }

            // Schedule auto fade out after display duration
            float delay = (Settings != null && Settings.ResultDisplayDurationSeconds > 0)
                ? Settings.ResultDisplayDurationSeconds
                : DisplayDurationSeconds;

            yield return new WaitForSeconds(delay);

            HideResult();
        }

        public void HideResult()
        {
            if (_resultSequenceCoroutine != null)
            {
                StopCoroutine(_resultSequenceCoroutine);
                _resultSequenceCoroutine = null;
            }

            if (ViewCanvasGroup != null)
            {
                ViewCanvasGroup.DOFade(0f, 0.3f, Ease.InQuad);
            }

            if (ModifierCardsRow != null)
                ModifierCardsRow.SetActive(false);
        }

        private void UpdateModifierCards(FinalRoll roll)
        {
            if (ResultCardsContainer == null && ModifierCardsRow != null)
            {
                var sr = ModifierCardsRow.GetComponentInChildren<ScrollRect>(true);
                if (sr != null && sr.content != null)
                {
                    ResultCardsContainer = sr.content;
                    ResultCardsScrollRect = sr;
                }
            }

            if (ResultCardsContainer != null)
            {
                // Clear old spawned card boxes
                foreach (Transform child in ResultCardsContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }

                var modsToDisplay = roll.AppliedModifiers;
                if (modsToDisplay != null && modsToDisplay.Count > 0)
                {
                    foreach (var mod in modsToDisplay)
                    {
                        if (mod == null) continue;
                        CreateResultCardBox(ResultCardsContainer, mod.Name, mod.Value);
                    }
                }
                else
                {
                    // Fallback single modifier card if no list
                    CreateResultCardBox(ResultCardsContainer, "MODIFIER", roll.Modifier);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(ResultCardsContainer as RectTransform);
            }
        }

        private void CreateResultCardBox(Transform parent, string title, int value)
        {
            GameObject cardObj = new GameObject("Card_" + title, typeof(RectTransform), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 65f);

            Image cardBg = cardObj.GetComponent<Image>();
            cardBg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            Outline cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.85f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Header Label (e.g. ATHLETICS, WISDOM, PROFICIENCY, GUIDANCE, BLESS)
            GameObject headerObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.55f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.offsetMin = new Vector2(4f, 0f);
            hRect.offsetMax = new Vector2(-4f, -4f);
            TextMeshProUGUI hTMP = headerObj.GetComponent<TextMeshProUGUI>();
            hTMP.text = string.IsNullOrEmpty(title) ? "MODIFIER" : title.ToUpper();
            hTMP.fontSize = 11;
            hTMP.fontStyle = FontStyles.Bold;
            hTMP.color = new Color(0.95f, 0.78f, 0.35f, 0.9f);
            hTMP.alignment = TextAlignmentOptions.Center;

            // Value Text (e.g. +2, +1, +0, -1) with floating animation
            GameObject valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(cardObj.transform, false);
            RectTransform vRect = valObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0f, 0f);
            vRect.anchorMax = new Vector2(1f, 0.55f);
            vRect.offsetMin = new Vector2(4f, 2f);
            vRect.offsetMax = new Vector2(-4f, 0f);
            TextMeshProUGUI vTMP = valObj.GetComponent<TextMeshProUGUI>();
            vTMP.text = (value >= 0 ? "+" : "") + value;
            vTMP.fontSize = 20;
            vTMP.fontStyle = FontStyles.Bold;
            vTMP.color = Color.white;
            vTMP.alignment = TextAlignmentOptions.Center;

            // Floating upward DOTween animation
            Vector2 targetPos = vRect.anchoredPosition;
            vRect.anchoredPosition = targetPos - new Vector2(0f, 15f);
            vRect.DOAnchorPos(targetPos, 0.35f, Ease.OutBack);
            vTMP.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f);
        }

        private Color GetBadgeColor(FinalRoll roll)
        {
            if (Settings != null)
            {
                if (roll.IsCriticalSuccess) return Settings.CriticalSuccessColor;
                if (roll.IsCriticalFailure) return Settings.CriticalFailureColor;
                if (roll.IsSuccess) return Settings.SuccessColor;
                return Settings.FailureColor;
            }

            if (roll.IsCriticalSuccess) return new Color(1f, 0.84f, 0f);
            if (roll.IsCriticalFailure) return new Color(0.9f, 0.15f, 0.15f);
            if (roll.IsSuccess) return new Color(0.2f, 0.85f, 0.35f);
            return new Color(0.85f, 0.3f, 0.2f);
        }
    }
}
