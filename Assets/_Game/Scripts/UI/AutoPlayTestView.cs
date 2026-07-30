using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Testing;

namespace BG3DiceSystem.UI
{
    public class AutoPlayTestView : MonoBehaviour
    {
        #region Events
        public event Action OnStartTestsRequested;
        public event Action OnStopTestsRequested;
        public event Action<float> OnWaitTimeChanged;
        #endregion

        #region Inspector References - Controls & Overlay
        [Header("Canvas & Overlay Container")]
        public CanvasGroup MainCanvasGroup;
        public RectTransform MainPanelRect;

        [Header("Header & Control Buttons")]
        public TextMeshProUGUI TitleText;
        public Button StartTestsButton;
        public Button StopTestsButton;
        public Button CloseViewButton;
        public Slider WaitTimeSlider;
        public TextMeshProUGUI WaitTimeLabelText;

        [Header("Progress Bar & Status Banner")]
        public GameObject ProgressContainer;
        public Slider ProgressBarSlider;
        public TextMeshProUGUI ProgressStatusText;
        public TextMeshProUGUI ProgressPercentText;

        [Header("Report Summary Banner")]
        public GameObject SummaryBannerContainer;
        public TextMeshProUGUI SummaryStatsText;
        public TextMeshProUGUI PassRatePercentText;
        public Image PassRateCircleImage;
        public Button CopyMarkdownButton;

        [Header("Checklist Scroll View")]
        public ScrollRect ChecklistScrollRect;
        public Transform ChecklistItemContainer;
        #endregion

        #region Private Fields
        private readonly List<GameObject> _spawnedItemRows = new List<GameObject>();
        private bool _isInitialized;
        #endregion

        #region Initialization & Setup
        public void InitializeView()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EnsureContainerLayout();
            BindUIListeners();

            if (MainCanvasGroup != null)
            {
                MainCanvasGroup.alpha = 0f;
                MainCanvasGroup.interactable = false;
                MainCanvasGroup.blocksRaycasts = false;
            }
            if (SummaryBannerContainer != null) SummaryBannerContainer.SetActive(false);
            if (ProgressContainer != null) ProgressContainer.SetActive(false);
            if (StopTestsButton != null) StopTestsButton.gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (!_isInitialized) InitializeView();
        }

        private void BindUIListeners()
        {
            if (StartTestsButton != null)
            {
                StartTestsButton.onClick.RemoveAllListeners();
                StartTestsButton.onClick.AddListener(() => OnStartTestsRequested?.Invoke());
            }
            if (StopTestsButton != null)
            {
                StopTestsButton.onClick.RemoveAllListeners();
                StopTestsButton.onClick.AddListener(() => OnStopTestsRequested?.Invoke());
            }
            if (CloseViewButton != null)
            {
                CloseViewButton.onClick.RemoveAllListeners();
                CloseViewButton.onClick.AddListener(HideView);
            }
            if (WaitTimeSlider != null)
            {
                WaitTimeSlider.onValueChanged.RemoveAllListeners();
                WaitTimeSlider.onValueChanged.AddListener((val) => {
                    UpdateWaitTimeLabel(val);
                    OnWaitTimeChanged?.Invoke(val);
                });
                UpdateWaitTimeLabel(WaitTimeSlider.value);
            }
            if (CopyMarkdownButton != null)
            {
                CopyMarkdownButton.onClick.RemoveAllListeners();
                CopyMarkdownButton.onClick.AddListener(CopyReportToClipboard);
            }
        }

        private void UpdateWaitTimeLabel(float seconds)
        {
            if (WaitTimeLabelText != null)
            {
                WaitTimeLabelText.text = $"Delay: {seconds:F1}s";
            }
        }
        #endregion

        #region Public API Display Controls
        public void ShowView()
        {
            InitializeView();
            gameObject.SetActive(true);
            if (MainCanvasGroup != null)
            {
                MainCanvasGroup.gameObject.SetActive(true);
                MainCanvasGroup.interactable = true;
                MainCanvasGroup.blocksRaycasts = true;
                MainCanvasGroup.DOFade(1f, 0.35f, Ease.OutQuad);
            }
            if (MainPanelRect != null)
            {
                MainPanelRect.DOPunchScale(Vector3.one * 0.05f, 0.3f);
            }
        }

        public void HideView()
        {
            if (MainCanvasGroup != null)
            {
                MainCanvasGroup.DOFade(0f, 0.25f, Ease.InQuad).OnComplete(() => {
                    MainCanvasGroup.interactable = false;
                    MainCanvasGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetRunningState(bool isRunning)
        {
            if (StartTestsButton != null) StartTestsButton.gameObject.SetActive(!isRunning);
            if (StopTestsButton != null) StopTestsButton.gameObject.SetActive(isRunning);
            if (WaitTimeSlider != null) WaitTimeSlider.interactable = !isRunning;
            if (ProgressContainer != null) ProgressContainer.SetActive(isRunning);

            if (isRunning)
            {
                ClearChecklistRows();
                if (SummaryBannerContainer != null) SummaryBannerContainer.SetActive(false);
                if (ProgressBarSlider != null) ProgressBarSlider.value = 0f;
                if (ProgressStatusText != null) ProgressStatusText.text = "Initializing Automated Test Suite...";
                if (ProgressPercentText != null) ProgressPercentText.text = "0%";
            }
        }

        public void UpdateProgress(int currentStep, int totalSteps, TestCaseResult stepResult)
        {
            float progress = totalSteps > 0 ? (float)currentStep / totalSteps : 0f;
            if (ProgressBarSlider != null) ProgressBarSlider.value = progress;
            if (ProgressPercentText != null) ProgressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";

            if (ProgressStatusText != null && stepResult != null)
            {
                string statusSymbol = stepResult.IsPassed ? "✅" : "❌";
                ProgressStatusText.text = $"Running [{currentStep}/{totalSteps}]: {stepResult.TestName} ({statusSymbol})";
            }

            AddChecklistRow(stepResult);
        }

        public void DisplayFinalReport(TestReport report)
        {
            SetRunningState(false);
            if (ProgressContainer != null) ProgressContainer.SetActive(false);
            if (SummaryBannerContainer != null) SummaryBannerContainer.SetActive(true);

            if (SummaryStatsText != null)
            {
                SummaryStatsText.text = $"Total: {report.TotalTests} | Passed: {report.PassedCount} | Failed: {report.FailedCount} | Duration: {report.TotalDurationSeconds:F1}s";
            }

            if (PassRatePercentText != null)
            {
                PassRatePercentText.text = $"{report.PassPercentage:F1}%";
                PassRatePercentText.color = report.FailedCount == 0 
                    ? new Color(0.2f, 0.85f, 0.35f, 1f) 
                    : new Color(0.9f, 0.3f, 0.2f, 1f);
            }

            if (SummaryBannerContainer != null)
            {
                SummaryBannerContainer.transform.DOPunchScale(Vector3.one * 0.1f, 0.4f);
            }
        }
        #endregion

        #region Checklist Row Item Creation
        private void ClearChecklistRows()
        {
            foreach (var row in _spawnedItemRows)
            {
                if (row != null) Destroy(row);
            }
            _spawnedItemRows.Clear();

            if (ChecklistItemContainer != null)
            {
                foreach (Transform child in ChecklistItemContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void AddChecklistRow(TestCaseResult result)
        {
            if (ChecklistItemContainer == null || result == null) return;

            GameObject rowObj = new GameObject($"Row_{result.TestName}", typeof(RectTransform), typeof(Image), typeof(Outline));
            rowObj.transform.SetParent(ChecklistItemContainer, false);

            RectTransform rect = rowObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 52f);

            Image bg = rowObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            Outline outline = rowObj.GetComponent<Outline>();
            outline.effectColor = result.IsPassed ? new Color(0.2f, 0.85f, 0.35f, 0.6f) : new Color(0.9f, 0.25f, 0.25f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Status Badge (Left)
            GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(rowObj.transform, false);
            RectTransform bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0.5f);
            bRect.anchorMax = new Vector2(0f, 0.5f);
            bRect.pivot = new Vector2(0f, 0.5f);
            bRect.anchoredPosition = new Vector2(8f, 0f);
            bRect.sizeDelta = new Vector2(75f, 32f);

            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = result.IsPassed ? new Color(0.15f, 0.45f, 0.25f, 0.95f) : new Color(0.55f, 0.15f, 0.15f, 0.95f);

            GameObject bLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            bLabelObj.transform.SetParent(badgeObj.transform, false);
            RectTransform blRect = bLabelObj.GetComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero; blRect.anchorMax = Vector2.one; blRect.offsetMin = Vector2.zero; blRect.offsetMax = Vector2.zero;
            TextMeshProUGUI blTMP = bLabelObj.GetComponent<TextMeshProUGUI>();
            blTMP.text = result.IsPassed ? "PASS ✓" : "FAIL ✗";
            blTMP.fontSize = 13;
            blTMP.fontStyle = FontStyles.Bold;
            blTMP.color = Color.white;
            blTMP.alignment = TextAlignmentOptions.Center;

            // Test Info & Metrics (Middle)
            GameObject infoObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoObj.transform.SetParent(rowObj.transform, false);
            RectTransform iRect = infoObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0f);
            iRect.anchorMax = new Vector2(0.72f, 1f);
            iRect.offsetMin = new Vector2(90f, 4f);
            iRect.offsetMax = new Vector2(0f, -4f);
            TextMeshProUGUI iTMP = infoObj.GetComponent<TextMeshProUGUI>();

            string diceValuesStr = (result.RollMode == RollMode.AdvantageTwoDice) 
                ? $"[{result.DiceValueA}, {result.DiceValueB}] -> {result.SelectedDiceValue}" 
                : $"{result.SelectedDiceValue}";

            iTMP.text = $"<b>{result.TestName}</b>\n<size=11><color=#B0B0C0>{result.DiceType} ({result.RollMode}) | Roll: {diceValuesStr} +{result.Modifier} = {result.Total} (DC {result.DifficultyClass})</color></size>";
            iTMP.fontSize = 13;
            iTMP.color = Color.white;
            iTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Outcome & Duration (Right)
            GameObject outcomeObj = new GameObject("OutcomeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            outcomeObj.transform.SetParent(rowObj.transform, false);
            RectTransform oRect = outcomeObj.GetComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0.73f, 0f);
            oRect.anchorMax = new Vector2(1f, 1f);
            oRect.offsetMin = new Vector2(0f, 4f);
            oRect.offsetMax = new Vector2(-8f, -4f);
            TextMeshProUGUI oTMP = outcomeObj.GetComponent<TextMeshProUGUI>();

            Color outcomeColor = result.IsCriticalSuccess ? new Color(1f, 0.84f, 0f) :
                                 (result.IsCriticalFailure ? new Color(0.9f, 0.2f, 0.2f) :
                                 (result.IsSuccess ? new Color(0.2f, 0.85f, 0.35f) : new Color(0.85f, 0.35f, 0.2f)));

            oTMP.text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(outcomeColor)}>{result.OutcomeText}</color></b>\n<size=10><color=#9090A0>{result.DurationSeconds:F2}s</color></size>";
            oTMP.fontSize = 12;
            oTMP.alignment = TextAlignmentOptions.MidlineRight;

            rowObj.transform.DOPunchScale(Vector3.one * 0.05f, 0.25f);
            _spawnedItemRows.Add(rowObj);

            // Auto scroll to bottom item
            Canvas.ForceUpdateCanvases();
            if (ChecklistScrollRect != null) ChecklistScrollRect.verticalNormalizedPosition = 0f;
        }
        #endregion

        #region Helpers & Layout
        private void EnsureContainerLayout()
        {
            if (ChecklistItemContainer != null)
            {
                var vert = ChecklistItemContainer.GetComponent<VerticalLayoutGroup>();
                if (vert == null) vert = ChecklistItemContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                vert.spacing = 6f;
                vert.padding = new RectOffset(6, 6, 6, 6);
                vert.childControlWidth = true;
                vert.childControlHeight = false;
                vert.childForceExpandWidth = true;
                vert.childForceExpandHeight = false;

                var csf = ChecklistItemContainer.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = ChecklistItemContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void CopyReportToClipboard()
        {
            var runner = GetComponent<AutoPlayTestRunner>();
            if (runner != null && runner.CurrentReport != null)
            {
                string md = runner.CurrentReport.GenerateMarkdownReport();
                GUIUtility.systemCopyBuffer = md;
                Debug.Log("[AutoPlayTestView] Markdown Test Report copied to system clipboard!");
            }
        }
        #endregion
    }
}
