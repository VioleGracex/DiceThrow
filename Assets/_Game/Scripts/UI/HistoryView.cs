using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.UI
{
    public class HistoryView : MonoBehaviour
    {
        [Header("Container")]
        public Transform ItemContainer;
        public GameObject HistoryItemPrefab;
        public Button ClearHistoryButton;

        private ILocalizationService _localizationService;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        public void SetLocalizationService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            RefreshLocalization();
        }

        public void RefreshLocalization()
        {
            if (ClearHistoryButton != null)
            {
                var label = ClearHistoryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = _localizationService != null ? _localizationService.GetText("history_clear") : "Clear History";
                }
            }
        }

        private void Awake()
        {
            if (ClearHistoryButton != null)
            {
                ClearHistoryButton.onClick.AddListener(ClearItems);
            }
        }

        public void AddHistoryEntry(FinalRoll roll)
        {
            if (ItemContainer == null || HistoryItemPrefab == null) return;

            GameObject itemObj = Instantiate(HistoryItemPrefab, ItemContainer);
            itemObj.transform.SetAsFirstSibling();

            TextMeshProUGUI label = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string modSign = roll.Modifier >= 0 ? "+" : "";
                string successStr = _localizationService != null ? _localizationService.GetText("history_success") : "SUCCESS";
                string failStr = _localizationService != null ? _localizationService.GetText("history_failure") : "FAILURE";
                string critSuccessStr = _localizationService != null ? _localizationService.GetText("history_crit_success") : "CRIT SUCCESS";
                string critFailStr = _localizationService != null ? _localizationService.GetText("history_crit_failure") : "CRIT FAIL";

                string statusText = roll.IsSuccess ? $"<color=#32CD32>{successStr}</color>" : $"<color=#FF4500>{failStr}</color>";
                if (roll.IsCriticalSuccess) statusText = $"<color=#FFD700>{critSuccessStr}</color>";
                if (roll.IsCriticalFailure) statusText = $"<color=#FF0000>{critFailStr}</color>";

                label.text = $"{roll.SelectedDiceValue} {modSign}{roll.Modifier} = {roll.Total}  {statusText}";
            }

            itemObj.transform.localScale = new Vector3(1f, 0f, 1f);
            itemObj.transform.DOScale(Vector3.one, 0.3f, Ease.OutBack);

            _spawnedItems.Add(itemObj);
        }

        public void ClearItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null) Destroy(item);
            }
            _spawnedItems.Clear();
        }
    }
}
