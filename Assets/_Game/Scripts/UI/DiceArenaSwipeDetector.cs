using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BG3DiceSystem.UI
{
    public class DiceArenaSwipeDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler
    {
        public event Action OnRollRequested;

        [Header("Settings")]
        public float SwipeThresholdPixels = 12f;
        public bool IsEnabled = true;
        public float CooldownDuration = 5.0f;

        private Vector2 _pointerDownPos;
        private bool _isPointerDown;
        private bool _swipeTriggered;
        private float _lastTriggerTime = -999f;

        private bool CanTrigger()
        {
            if (!IsEnabled) return false;
            if (Time.time - _lastTriggerTime < CooldownDuration)
            {
                Debug.Log($"[DiceArenaSwipeDetector] Trigger ignored: Cooldown active ({CooldownDuration - (Time.time - _lastTriggerTime):F1}s remaining).");
                return false;
            }
            return true;
        }

        private void TriggerRoll()
        {
            if (!CanTrigger()) return;
            _lastTriggerTime = Time.time;
            OnRollRequested?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsEnabled) return;
            _pointerDownPos = eventData.position;
            _isPointerDown = true;
            _swipeTriggered = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsEnabled || !_isPointerDown || _swipeTriggered) return;

            float dist = Vector2.Distance(eventData.position, _pointerDownPos);
            if (dist >= SwipeThresholdPixels)
            {
                _swipeTriggered = true;
                _isPointerDown = false;
                Debug.Log($"[DiceArenaSwipeDetector] Swipe detected! Distance: {dist:F1}px");
                TriggerRoll();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsEnabled || _swipeTriggered || eventData.dragging) return;
            Debug.Log("[DiceArenaSwipeDetector] Pointer Click detected!");
            TriggerRoll();
        }
    }
}
