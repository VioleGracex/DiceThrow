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
        public TextMeshProUGUI DieAText;
        public TextMeshProUGUI DieBText;
        public TextMeshProUGUI TakenText;
        public TextMeshProUGUI ModifierText;
        public TextMeshProUGUI TotalText;
        public TextMeshProUGUI StatusBadgeText;
        public Image StatusBadgeBackground;

        [Header("Settings Reference")]
        public RollSettingsSO Settings;

        private void Awake()
        {
            if (ViewCanvasGroup != null) ViewCanvasGroup.alpha = 0f;
        }

        public void DisplayResult(FinalRoll roll)
        {
            if (ViewCanvasGroup == null) return;

            if (DieBContainer != null)
            {
                DieBContainer.SetActive(roll.Mode == RollMode.AdvantageTwoDice);
            }

            if (DieAText != null) DieAText.text = roll.DiceValueA.ToString();
            if (DieBText != null) DieBText.text = roll.DiceValueB.ToString();

            if (TakenText != null)
            {
                TakenText.text = roll.SelectedDiceValue.ToString() +
                    (roll.Mode == RollMode.AdvantageTwoDice ? " (HIGHEST)" : "");
            }

            if (ModifierText != null)
            {
                ModifierText.text = (roll.Modifier >= 0 ? "+" : "") + roll.Modifier.ToString();
            }

            if (TotalText != null)
            {
                TotalText.text = roll.Total.ToString();
            }

            Color badgeColor = GetBadgeColor(roll);
            string badgeText = "SUCCESS";

            if (roll.IsCriticalSuccess)
            {
                badgeText = "CRITICAL SUCCESS!";
            }
            else if (roll.IsCriticalFailure)
            {
                badgeText = "CRITICAL FAILURE!";
            }
            else if (roll.IsSuccess)
            {
                badgeText = "SUCCESS";
            }
            else
            {
                badgeText = "FAILURE";
            }

            if (StatusBadgeText != null)
            {
                StatusBadgeText.text = badgeText;
                StatusBadgeText.color = badgeColor;
            }

            if (StatusBadgeBackground != null)
            {
                StatusBadgeBackground.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.25f);
            }

            ViewCanvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.85f;

            CustomSequence seq = CustomDOTween.Sequence();
            seq.Append(ViewCanvasGroup.DOFade(1f, 0.4f, Ease.OutQuad));
            seq.Append(transform.DOScale(1f, 0.4f, Ease.OutBack));
            if (StatusBadgeText != null)
            {
                seq.Append(StatusBadgeText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f));
            }
        }

        public void HideResult()
        {
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
