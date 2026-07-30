using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using BG3DiceSystem.Gameplay.Dice;

namespace BG3DiceSystem.UI
{
    public class DiceDirectRaycastDetector : MonoBehaviour
    {
        public event Action OnRollRequested;

        [Header("Settings")]
        public Camera OverlayCamera;
        public LayerMask DiceLayerMask = -1;
        public bool IsEnabled = true;
        public float CooldownDuration = 5.0f;

        private float _cooldownTimer = 0f;

        public bool IsInCooldown => _cooldownTimer > 0f;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        public void TriggerCooldown(float duration = -1f)
        {
            float time = duration > 0f ? duration : CooldownDuration;
            _cooldownTimer = time;
            Debug.Log($"[DiceRaycast] Detector cooldown started for {time} seconds.");
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer > 0f) return;
            }

            if (!IsEnabled) return;

            var uiController = UnityEngine.Object.FindFirstObjectByType<BG3DiceSystem.UI.UIController>();
            if (uiController != null && uiController.IsRolling) return;

            // If pointer is over any UI element (e.g. scrolling left panel, dropdown, cards), do not trigger roll!
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (OverlayCamera == null)
            {
                var camObj = GameObject.Find("Overlay Dice Camera");
                if (camObj != null) OverlayCamera = camObj.GetComponent<Camera>();
                if (OverlayCamera == null) OverlayCamera = Camera.main;
            }

            if (OverlayCamera == null) return;

            var pointer = Pointer.current;
            if (pointer == null) return;

            // Detect Left Mouse Button (LMB) / Touch Press
            if (!pointer.press.wasPressedThisFrame && !pointer.press.wasReleasedThisFrame) return;

            Vector2 screenPos = pointer.position.ReadValue();
            Ray ray = OverlayCamera.ScreenPointToRay(screenPos);

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, DiceLayerMask);
            DiceController foundDie = null;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                
                // Ignore boundary walls
                if (hit.collider.gameObject.name.Contains("Wall") || hit.collider.gameObject.name.Contains("Boundary")) continue;

                var dc = hit.collider.GetComponentInParent<DiceController>();
                if (dc != null && dc.gameObject.activeInHierarchy)
                {
                    foundDie = dc;
                    break;
                }
            }

            // Single Source of Truth: Trigger roll if cursor/pointer is directly over any 3D die and system is not rolling
            if (foundDie != null)
            {
                Debug.Log($"[DiceRaycast] Direct click/touch on die '{foundDie.gameObject.name}'! Triggering roll... Cooldown 5s started.");
                TriggerCooldown(CooldownDuration);
                OnRollRequested?.Invoke();
            }
        }
    }
}
