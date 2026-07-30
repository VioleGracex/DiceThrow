using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;

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
        private readonly List<(GameObject cardObj, string title, int value)> _activeSpawnedCards = new List<(GameObject cardObj, string title, int value)>();

        private void Awake()
        {
            if (ViewCanvasGroup != null)
            {
                ViewCanvasGroup.DOKill();
                ViewCanvasGroup.alpha = 1f;
            }

            EnsureModifierRowLayout();

            if (TotalText != null) TotalText.gameObject.SetActive(false);
            if (TakenText != null) TakenText.gameObject.SetActive(false);
            if (StatusBadgeText != null) StatusBadgeText.gameObject.SetActive(false);
            if (StatusBadgeBackground != null) StatusBadgeBackground.gameObject.SetActive(false);

            if (ModifierCardsRow != null)
            {
                ModifierCardsRow.transform.DOKill();
                ModifierCardsRow.transform.localScale = Vector3.one;
                ModifierCardsRow.SetActive(false);
            }
        }

        public void EnsureModifierRowLayout()
        {
            if (ModifierCardsRow == null) return;

            RectTransform rowRect = ModifierCardsRow.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                // Make modifier row wider (width 560f)
                rowRect.sizeDelta = new Vector2(560f, 75f);
            }

            if (ResultCardsScrollRect == null)
            {
                ResultCardsScrollRect = ModifierCardsRow.GetComponentInChildren<ScrollRect>(true);
            }

            if (ResultCardsScrollRect != null)
            {
                ResultCardsScrollRect.horizontal = true;
                ResultCardsScrollRect.vertical = false;
                ResultCardsScrollRect.movementType = ScrollRect.MovementType.Clamped;

                if (ResultCardsScrollRect.viewport != null)
                {
                    RectTransform vpRect = ResultCardsScrollRect.viewport;
                    vpRect.anchorMin = Vector2.zero;
                    vpRect.anchorMax = Vector2.one;
                    vpRect.sizeDelta = Vector2.zero;
                    vpRect.anchoredPosition = Vector2.zero;
                }

                if (ResultCardsContainer == null && ResultCardsScrollRect.content != null)
                {
                    ResultCardsContainer = ResultCardsScrollRect.content;
                }
            }

            if (ResultCardsContainer != null)
            {
                var hlg = ResultCardsContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = ResultCardsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.padding = new RectOffset(10, 10, 5, 5);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                var csf = ResultCardsContainer.GetComponent<ContentSizeFitter>();
                if (csf == null) csf = ResultCardsContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        public void RefreshModifierCards(IReadOnlyList<ModifierData> activeModifiers, int baseModifier)
        {
            EnsureModifierRowLayout();

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
                // Build list of target items (title, value)
                List<(string title, int value)> targetItems = new List<(string title, int value)>();

                if (baseModifier != 0)
                {
                    targetItems.Add(("BONUS", baseModifier));
                }

                if (activeModifiers != null)
                {
                    foreach (var mod in activeModifiers)
                    {
                        if (mod != null && mod.Value != 0)
                        {
                            targetItems.Add((mod.Name, mod.Value));
                        }
                    }
                }

                // Map existing child cards by title
                Dictionary<string, GameObject> existingCards = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);
                List<GameObject> childrenToRemove = new List<GameObject>();

                foreach (Transform child in ResultCardsContainer)
                {
                    if (child == null) continue;
                    string name = child.gameObject.name;
                    string titleKey = name.StartsWith("Card_") ? name.Substring(5) : name;

                    if (targetItems.Exists(t => t.title.Equals(titleKey, System.StringComparison.OrdinalIgnoreCase)) && !existingCards.ContainsKey(titleKey))
                    {
                        existingCards[titleKey] = child.gameObject;
                    }
                    else
                    {
                        childrenToRemove.Add(child.gameObject);
                    }
                }

                // Destroy cards that are no longer active
                foreach (var obj in childrenToRemove)
                {
                    obj.transform.DOKill();
                    if (Application.isPlaying) Destroy(obj);
                    else DestroyImmediate(obj);
                }

                _activeSpawnedCards.Clear();

                // Re-arrange / Instantiate target cards
                for (int i = 0; i < targetItems.Count; i++)
                {
                    var item = targetItems[i];
                    GameObject cardObj = null;

                    if (existingCards.TryGetValue(item.title, out GameObject existingObj) && existingObj != null)
                    {
                        // Existing card: keep in place, update value text, DO NOT re-animate spawn
                        cardObj = existingObj;
                        cardObj.transform.SetSiblingIndex(i);

                        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                        cardObj.transform.localScale = Vector3.one;

                        var valTMP = cardObj.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
                        if (valTMP != null)
                        {
                            string newText = (item.value >= 0 ? "+" : "") + item.value;
                            if (valTMP.text != newText)
                            {
                                valTMP.text = newText;
                            }
                        }
                    }
                    else
                    {
                        // NEW card: create and animate pop-in ONLY for this newly added card!
                        cardObj = CreateResultCardBox(ResultCardsContainer, item.title, item.value);
                        cardObj.transform.SetSiblingIndex(i);
                        AnimateCardSpawn(cardObj);
                    }

                    _activeSpawnedCards.Add((cardObj, item.title, item.value));
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(ResultCardsContainer as RectTransform);

                if (_activeSpawnedCards.Count > 0 && ModifierCardsRow != null)
                {
                    ModifierCardsRow.SetActive(true);
                    ModifierCardsRow.transform.DOKill();
                    ModifierCardsRow.transform.localScale = Vector3.one;
                }
                else if (ModifierCardsRow != null)
                {
                    ModifierCardsRow.SetActive(false);
                }
            }
        }

        private void AnimateCardSpawn(GameObject cardObj)
        {
            if (cardObj == null) return;

            CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.DOKill();
            cg.DOFade(1f, 0.35f, Ease.OutQuad);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.DOKill();
                Vector2 finalPos = rect.anchoredPosition;
                rect.anchoredPosition = new Vector2(finalPos.x, finalPos.y - 18f);
                rect.DOAnchorPosY(finalPos.y, 0.35f, Ease.OutQuad);
                rect.localScale = Vector3.one * 0.85f;
                rect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutQuad);
            }
        }

        public void DisplayResult(FinalRoll roll)
        {
            if (ViewCanvasGroup == null) return;

            Debug.Log($"[RollResult] Raw Dice: {roll.SelectedDiceValue} (DieA={roll.DiceValueA}, DieB={roll.DiceValueB}), Modifier: +{roll.Modifier}, DC: {roll.DifficultyClass}, Total: {roll.Total}, Outcome: {(roll.IsCriticalSuccess ? "CRITICAL SUCCESS" : (roll.IsCriticalFailure ? "CRITICAL FAILURE" : (roll.IsSuccess ? "SUCCESS" : "FAILURE")))}");

            if (_resultSequenceCoroutine != null)
            {
                StopCoroutine(_resultSequenceCoroutine);
                _resultSequenceCoroutine = null;
            }

            ViewCanvasGroup.DOKill();
            ViewCanvasGroup.alpha = 1f;

            if (TotalText != null)
            {
                TotalText.transform.DOKill();
                TotalText.transform.localScale = Vector3.one;
            }
            if (StatusBadgeBackground != null)
            {
                StatusBadgeBackground.transform.DOKill();
                StatusBadgeBackground.transform.localScale = Vector3.one;
            }
            if (ModifierCardsRow != null)
            {
                ModifierCardsRow.transform.DOKill();
                ModifierCardsRow.transform.localScale = Vector3.one;
            }

            _resultSequenceCoroutine = StartCoroutine(AnimateBG3ResultSequence(roll));
        }

        private IEnumerator AnimateBG3ResultSequence(FinalRoll roll)
        {
            ViewCanvasGroup.alpha = 1f;

            if (StatusBadgeText != null) StatusBadgeText.gameObject.SetActive(false);
            if (StatusBadgeBackground != null) StatusBadgeBackground.gameObject.SetActive(false);

            // 1. Show initial raw die result
            int rawVal = roll.SelectedDiceValue;
            if (TotalText != null)
            {
                TotalText.text = rawVal.ToString();
                TotalText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Calculate base bonus vs applied modifiers
            int activeSum = 0;
            if (roll.AppliedModifiers != null)
            {
                foreach (var mod in roll.AppliedModifiers)
                {
                    if (mod != null) activeSum += mod.Value;
                }
            }
            int baseBonus = roll.Modifier - activeSum;

            // Populate & reveal modifier cards row
            RefreshModifierCards(roll.AppliedModifiers, baseBonus);
            bool hasModifiers = _activeSpawnedCards.Count > 0;

            yield return new WaitForSeconds(0.35f);

            // 3. Sequentially fly each modifier card to TotalText
            if (hasModifiers && TotalText != null)
            {
                int currentVal = rawVal;
                foreach (var cardData in _activeSpawnedCards)
                {
                    if (cardData.cardObj == null) continue;

                    RectTransform cardRect = cardData.cardObj.GetComponent<RectTransform>();
                    if (cardRect != null)
                    {
                        yield return StartCoroutine(ScrollToCardCoroutine(cardRect));
                    }

                    // Highlight active card box smoothly without shake
                    cardData.cardObj.transform.DOKill();
                    cardData.cardObj.transform.DOScale(Vector3.one * 1.1f, 0.18f).SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        if (cardData.cardObj != null) cardData.cardObj.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutQuad);
                    });

                    // Create flying payload
                    GameObject flyObj = CreateFlyingPayload(cardData.value, cardData.cardObj.transform.position);

                    Vector3 startPos = cardData.cardObj.transform.position;
                    Vector3 targetPos = TotalText.transform.position;

                    float flyDuration = 0.4f;
                    float elapsed = 0f;

                    while (elapsed < flyDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / flyDuration;
                        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                        currentPos.y += Mathf.Sin(t * Mathf.PI) * 35f;
                        if (flyObj != null) flyObj.transform.position = currentPos;
                        yield return null;
                    }

                    if (flyObj != null) Destroy(flyObj);

                    currentVal += cardData.value;
                    TotalText.text = currentVal.ToString();

                    TotalText.transform.DOKill();
                    TotalText.transform.localScale = Vector3.one;
                    TotalText.transform.DOPunchScale(Vector3.one * 0.35f, 0.2f);

                    yield return new WaitForSeconds(0.25f);
                }
            }
            else if (TotalText != null)
            {
                TotalText.text = roll.Total.ToString();
            }

            yield return new WaitForSeconds(0.3f);

            // 4. Reveal Final Outcome Status Badge
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
                StatusBadgeBackground.transform.DOKill();
                StatusBadgeBackground.transform.localScale = Vector3.one;
                StatusBadgeBackground.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f);
            }

            float delay = (Settings != null && Settings.ResultDisplayDurationSeconds > 0)
                ? Settings.ResultDisplayDurationSeconds
                : DisplayDurationSeconds;

            yield return new WaitForSeconds(delay);

            HideResult();
        }

        private IEnumerator ScrollToCardCoroutine(RectTransform targetCard, float duration = 0.25f)
        {
            if (ResultCardsScrollRect == null || ResultCardsContainer == null || targetCard == null) yield break;

            RectTransform viewportRect = ResultCardsScrollRect.viewport != null 
                ? ResultCardsScrollRect.viewport 
                : (ResultCardsScrollRect.transform as RectTransform);

            if (viewportRect == null) yield break;

            RectTransform contentRect = ResultCardsContainer as RectTransform;
            if (contentRect == null) yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            float contentWidth = contentRect.rect.width;
            float viewportWidth = viewportRect.rect.width;

            if (contentWidth <= viewportWidth) yield break;

            Vector3[] viewportCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportCorners);
            float viewportCenterX = (viewportCorners[0].x + viewportCorners[2].x) * 0.5f;

            float cardCenterX = targetCard.position.x;
            float deltaX = viewportCenterX - cardCenterX;

            float targetAnchoredX = contentRect.anchoredPosition.x + deltaX;
            float minX = -(contentWidth - viewportWidth);
            float maxX = 0f;

            targetAnchoredX = Mathf.Clamp(targetAnchoredX, minX, maxX);

            float startX = contentRect.anchoredPosition.x;
            if (Mathf.Abs(startX - targetAnchoredX) < 2f) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float currentX = Mathf.Lerp(startX, targetAnchoredX, t);
                contentRect.anchoredPosition = new Vector2(currentX, contentRect.anchoredPosition.y);
                yield return null;
            }
            contentRect.anchoredPosition = new Vector2(targetAnchoredX, contentRect.anchoredPosition.y);
        }

        private GameObject CreateFlyingPayload(int value, Vector3 spawnWorldPos)
        {
            GameObject flyObj = new GameObject("FlyPayload_" + value, typeof(RectTransform), typeof(CanvasGroup));
            flyObj.transform.SetParent(ViewCanvasGroup != null ? ViewCanvasGroup.transform : transform, false);
            flyObj.transform.position = spawnWorldPos;

            RectTransform rect = flyObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 50f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Outline));
            textObj.transform.SetParent(flyObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            string valStr = (value >= 0 ? "+" : "") + value;
            tmp.text = valStr;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = value >= 0 ? new Color(0.95f, 0.78f, 0.35f, 1f) : new Color(0.95f, 0.35f, 0.35f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;

            Outline outline = textObj.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            flyObj.transform.localScale = Vector3.one * 1.2f;
            return flyObj;
        }

        public void HideResult()
        {
            if (_resultSequenceCoroutine != null)
            {
                StopCoroutine(_resultSequenceCoroutine);
                _resultSequenceCoroutine = null;
            }

            if (TotalText != null) TotalText.gameObject.SetActive(false);
            if (TakenText != null) TakenText.gameObject.SetActive(false);
            if (StatusBadgeText != null) StatusBadgeText.gameObject.SetActive(false);
            if (StatusBadgeBackground != null) StatusBadgeBackground.gameObject.SetActive(false);

            if (_activeSpawnedCards.Count > 0 && ModifierCardsRow != null)
            {
                ModifierCardsRow.SetActive(true);
                ModifierCardsRow.transform.DOKill();
                ModifierCardsRow.transform.localScale = Vector3.one;
            }
            else if (ModifierCardsRow != null)
            {
                ModifierCardsRow.SetActive(false);
            }
        }

        private GameObject CreateResultCardBox(Transform parent, string title, int value)
        {
            GameObject cardObj = new GameObject("Card_" + title, typeof(RectTransform), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130f, 52f);

            Image cardBg = cardObj.GetComponent<Image>();
            cardBg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            Outline cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.85f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Title Header Label (Top Half)
            GameObject headerObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.52f);
            hRect.anchorMax = new Vector2(1f, 0.98f);
            hRect.offsetMin = new Vector2(4f, 0f);
            hRect.offsetMax = new Vector2(-4f, 0f);
            TextMeshProUGUI hTMP = headerObj.GetComponent<TextMeshProUGUI>();
            hTMP.text = string.IsNullOrEmpty(title) ? "BONUS" : title.ToUpper();
            hTMP.fontSize = 11;
            hTMP.fontStyle = FontStyles.Bold;
            hTMP.color = new Color(0.95f, 0.78f, 0.35f, 1f);
            hTMP.alignment = TextAlignmentOptions.Center;
            hTMP.textWrappingMode = TextWrappingModes.NoWrap;
            hTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Value Text (Bottom Half)
            GameObject valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(cardObj.transform, false);
            RectTransform vRect = valObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0f, 0f);
            vRect.anchorMax = new Vector2(1f, 0.52f);
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            TextMeshProUGUI vTMP = valObj.GetComponent<TextMeshProUGUI>();
            vTMP.text = (value >= 0 ? "+" : "") + value;
            vTMP.fontSize = 18;
            vTMP.fontStyle = FontStyles.Bold;
            vTMP.color = Color.white;
            vTMP.alignment = TextAlignmentOptions.Center;
            return cardObj;
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
