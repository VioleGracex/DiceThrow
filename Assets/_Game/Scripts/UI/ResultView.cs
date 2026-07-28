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
        public GameObject DieAContainer;
        public GameObject DieBContainer;

        [Header("Value Texts")]
        public TextMeshProUGUI DCText;
        public TextMeshProUGUI DieAText;
        public TextMeshProUGUI DieBText;
        public TextMeshProUGUI TakenText;
        public TextMeshProUGUI ModifierText;
        public TextMeshProUGUI TotalText;
        public TextMeshProUGUI StatusBadgeText;
        public Image StatusBadgeBackground;

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

            // Ensure layout text is positioned away from 3D die area
            PositionUIElements();

            _resultSequenceCoroutine = StartCoroutine(AnimateBG3ResultSequence(roll));
        }

        private void PositionUIElements()
        {
            // Top header above die area (y = +210)
            if (DCText != null)
            {
                RectTransform rt = DCText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, 210f);
            }

            // Directly under die area (y = -110)
            if (TotalText != null)
            {
                RectTransform rt = TotalText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -110f);
            }

            // Advantage text under die (y = -140)
            if (TakenText != null)
            {
                RectTransform rt = TakenText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -140f);
            }

            // Bonus card area under die (y = -175)
            if (ModifierText != null)
            {
                RectTransform rt = ModifierText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -175f);
            }

            // Status outcome badge at bottom (y = -250)
            if (StatusBadgeBackground != null)
            {
                RectTransform rt = StatusBadgeBackground.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -250f);
            }
            else if (StatusBadgeText != null)
            {
                RectTransform rt = StatusBadgeText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -250f);
            }
        }

        private System.Collections.IEnumerator AnimateBG3ResultSequence(FinalRoll roll)
        {
            ViewCanvasGroup.alpha = 1f;

            // 1. Show Difficulty Class Header at top
            if (DCText != null)
            {
                DCText.text = $"DIFFICULTY CLASS {roll.DifficultyClass}";
                DCText.gameObject.SetActive(true);
            }

            // Hide status badge initially for sequential reveal
            if (StatusBadgeText != null) StatusBadgeText.gameObject.SetActive(false);
            if (StatusBadgeBackground != null) StatusBadgeBackground.gameObject.SetActive(false);

            // 2. Show initial raw die result right under the die
            int rawVal = roll.SelectedDiceValue;
            if (TotalText != null)
            {
                TotalText.text = rawVal.ToString();
                TotalText.gameObject.SetActive(true);
                TotalText.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f);
            }

            if (DieBContainer != null) DieBContainer.SetActive(roll.Mode == RollMode.AdvantageTwoDice);
            if (DieAText != null) DieAText.text = roll.DiceValueA.ToString();
            if (DieBText != null) DieBText.text = roll.DiceValueB.ToString();

            yield return new WaitForSeconds(0.4f);

            // 3. Show Modifier Bonus Card and animate bonus addition (+Modifier)
            if (roll.Modifier != 0)
            {
                if (ModifierText != null)
                {
                    ModifierText.text = $"+{roll.Modifier} Skill Bonus";
                    ModifierText.gameObject.SetActive(true);
                    ModifierText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                }

                yield return new WaitForSeconds(0.35f);

                // Animate value counting up from raw roll to final total
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
            else
            {
                if (ModifierText != null) ModifierText.gameObject.SetActive(false);
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
