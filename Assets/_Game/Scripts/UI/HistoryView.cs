using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

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
                string statusText = roll.IsSuccess ? "<color=#32CD32>SUCCESS</color>" : "<color=#FF4500>FAILURE</color>";
                if (roll.IsCriticalSuccess) statusText = "<color=#FFD700>CRIT SUCCESS</color>";
                if (roll.IsCriticalFailure) statusText = "<color=#FF0000>CRIT FAIL</color>";

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
