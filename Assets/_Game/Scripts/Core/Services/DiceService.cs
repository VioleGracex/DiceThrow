using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Utilities.Tweening;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;

namespace BG3DiceSystem.Core.Services
{
    public class DiceService : IDiceService
    {
        #region Events
        public event Action<Transform, float> OnDiceImpact;
        public event Action<DiceType> OnDiceTypeChanged;
        #endregion

        #region Private Fields
        private readonly Dictionary<DiceType, GameObject> _dicePrefabs;
        private readonly DiceSettingsSO _settings;
        private readonly List<DiceController> _spawnedDice = new List<DiceController>();
        private GameObject _previewDiceObj;
        private DiceType _currentDiceType = DiceType.D20;
        private bool _isRolling;
        #endregion

        #region Properties
        public bool IsRolling => _isRolling;

        public DiceType CurrentDiceType
        {
            get => _currentDiceType;
            set
            {
                Debug.Log($"[DiceService] CurrentDiceType set to: {value}");
                if (_currentDiceType != value || _previewDiceObj == null)
                {
                    _currentDiceType = value;
                    OnDiceTypeChanged?.Invoke(_currentDiceType);
                    SpawnPreviewDice(_currentDiceType);
                }
            }
        }
        #endregion

        #region Constructor & Initialization
        public DiceService(Dictionary<DiceType, GameObject> dicePrefabs, DiceSettingsSO settings)
        {
            _dicePrefabs = dicePrefabs ?? new Dictionary<DiceType, GameObject>();
            _settings = settings;
            Debug.Log($"[DiceService] Initialized with D20 default.");
        }
        #endregion

        #region Preview Dice Operations
        public void SpawnPreviewDice(DiceType type)
        {
            if (_isRolling) return;
            Debug.Log($"[DiceService] Spawning single preview dice of type: {type} in Overlay Space.");

            // Clean up existing preview die
            if (_previewDiceObj != null)
            {
                GameObject oldDie = _previewDiceObj;
                _previewDiceObj = null;
                if (oldDie != null) UnityEngine.Object.Destroy(oldDie);
            }

            GameObject prefabToSpawn = GetPrefab(type);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[DiceService] Prefab for {type} not found!");
                return;
            }

            // Position in isolated Overlay Camera Space (1000, 1000, 0)
            Vector3 floatPos = new Vector3(1000f, 1000f, 0f);
            Quaternion floatRot = Quaternion.Euler(0f, 0f, 0f);

            GameObject newDie = UnityEngine.Object.Instantiate(prefabToSpawn, floatPos, floatRot);
            int diceLayer = LayerMask.NameToLayer("Dice");
            int targetLayer = diceLayer != -1 ? diceLayer : 0;
            newDie.layer = targetLayer;
            foreach (Transform child in newDie.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = targetLayer;
            }

            Rigidbody rb = newDie.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Orient preview die face 1 to camera cleanly
            DiceController controller = newDie.GetComponent<DiceController>();
            if (controller != null)
            {
                newDie.transform.rotation = controller.CalculateFacingRotation(1, -Vector3.forward);
            }

            _previewDiceObj = newDie;
        }
        #endregion

        #region Roll Execution Operations (BG3 Authentic Unity Physics Roll & Dot-Product Detection)
        public async Task<List<int>> RollDiceAsync(RollMode mode)
        {
            if (_isRolling) return new List<int>();
            _isRolling = true;

            // Clean up preview die before rolling single die instance
            if (_previewDiceObj != null)
            {
                UnityEngine.Object.Destroy(_previewDiceObj);
                _previewDiceObj = null;
            }

            ClearActiveDice();

            int count = (mode == RollMode.AdvantageTwoDice) ? 2 : 1;
            List<DiceController> currentRollControllers = new List<DiceController>();
            List<int> results = new List<int>();

            Vector3 centerPos = new Vector3(1000f, 1000f, 0f);
            Vector3 spread = new Vector3(2.0f, 0f, 0f);

            GameObject prefabToSpawn = GetPrefab(_currentDiceType);
            if (prefabToSpawn == null)
            {
                _isRolling = false;
                return new List<int> { 20 };
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = centerPos + (i - (count - 1) * 0.5f) * spread;
                Quaternion spawnRot = UnityEngine.Random.rotation;

                GameObject diceObj = UnityEngine.Object.Instantiate(prefabToSpawn, spawnPos, spawnRot);

                int diceLayer = LayerMask.NameToLayer("Dice");
                int targetLayer = diceLayer != -1 ? diceLayer : 0;
                diceObj.layer = targetLayer;
                foreach (Transform child in diceObj.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = targetLayer;
                }

                DiceController controller = diceObj.GetComponent<DiceController>();
                if (controller == null)
                {
                    controller = diceObj.AddComponent<DiceController>();
                }

                controller.Initialize(_settings);
                controller.OnImpact += (t, force) => OnDiceImpact?.Invoke(t, force);

                _spawnedDice.Add(controller);
                currentRollControllers.Add(controller);
            }

            // 1. Throw dice with realistic horizontal impulse and torque under Unity physics
            foreach (var die in currentRollControllers)
            {
                if (die != null)
                {
                    die.ThrowDice();
                }
            }

            // 2. Wait until Rigidbody comes to complete rest under physics simulation
            float maxRollTimeout = 3.5f;
            float startTime = Time.time;

            while (Time.time - startTime < maxRollTimeout)
            {
                bool allSettled = true;
                foreach (var die in currentRollControllers)
                {
                    if (die != null && !die.IsSleeping())
                    {
                        allSettled = false;
                        break;
                    }
                }

                if (allSettled && (Time.time - startTime > 0.4f))
                {
                    break;
                }

                await Task.Yield();
            }

            // 3. Detect upward face value facing overlay camera (-Vector3.forward)
            Vector3 cameraDir = -Vector3.forward;

            for (int i = 0; i < currentRollControllers.Count; i++)
            {
                var die = currentRollControllers[i];
                if (die != null)
                {
                    if (die.RigidBody != null) die.RigidBody.isKinematic = true;

                    int rollVal = die.GetUpwardValue();
                    results.Add(rollVal);

                    Vector3 targetMiddlePos = centerPos + (i - (currentRollControllers.Count - 1) * 0.5f) * new Vector3(1.0f, 0f, 0f);
                    Quaternion targetRot = die.CalculateFacingRotation(rollVal, cameraDir);

                    // Smoothly return die to middle spot with result face facing camera square & upright
                    die.transform.DOMove(targetMiddlePos, 0.45f, Ease.OutQuad);
                    die.transform.DORotateQuaternion(targetRot, 0.45f, Ease.OutQuad);

                    OnDiceImpact?.Invoke(die.transform, 15f);
                }
            }

            _isRolling = false;

            // Re-spawn floating preview die after result display delay
            _ = RespawnPreviewAfterDelay(2.5f);

            return results;
        }

        private async Task RespawnPreviewAfterDelay(float delaySeconds)
        {
            await Task.Delay((int)(delaySeconds * 1000));
            if (!_isRolling && _spawnedDice.Count == 0)
            {
                SpawnPreviewDice(_currentDiceType);
            }
        }

        public void ClearActiveDice()
        {
            if (_previewDiceObj != null)
            {
                UnityEngine.Object.Destroy(_previewDiceObj);
                _previewDiceObj = null;
            }
            foreach (var die in _spawnedDice)
            {
                if (die != null && die.gameObject != null)
                {
                    UnityEngine.Object.Destroy(die.gameObject);
                }
            }
            _spawnedDice.Clear();
        }
        #endregion

        #region Helper Methods
        private GameObject GetPrefab(DiceType type)
        {
            if (_dicePrefabs.ContainsKey(type)) return _dicePrefabs[type];
            if (_dicePrefabs.Count > 0)
            {
                foreach (var kvp in _dicePrefabs) return kvp.Value;
            }
            return null;
        }
        #endregion
    }
}
