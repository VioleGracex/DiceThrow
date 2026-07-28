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
            Debug.Log($"[DiceService] Spawning preview dice of type: {type}");

            // Clean up any editor scene preview or active preview die
            var sceneDefault = GameObject.Find("Default_D20_Preview");
            if (sceneDefault != null && sceneDefault != _previewDiceObj)
            {
                UnityEngine.Object.Destroy(sceneDefault);
            }

            if (_previewDiceObj != null)
            {
                GameObject oldDie = _previewDiceObj;
                _previewDiceObj = null;
                oldDie.transform.DOScale(Vector3.zero, 0.2f, Ease.InBack).OnComplete(() => {
                    if (oldDie != null) UnityEngine.Object.Destroy(oldDie);
                });
            }

            GameObject prefabToSpawn = GetPrefab(type);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[DiceService] Prefab for {type} not found!");
                return;
            }

            Vector3 floatPos = new Vector3(0f, 1.6f, 0f);
            Quaternion floatRot = Quaternion.Euler(20f, 40f, 15f);

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

            // Animate scale pop for prominent center presentation
            newDie.transform.localScale = Vector3.zero;
            newDie.transform.DOScale(Vector3.one * 1.5f, 0.35f, Ease.OutBack);

            _previewDiceObj = newDie;
        }
        #endregion

        #region Roll Execution Operations
        public async Task<List<int>> RollDiceAsync(RollMode mode)
        {
            if (_isRolling) return new List<int>();
            _isRolling = true;

            // Destroy preview die
            if (_previewDiceObj != null)
            {
                UnityEngine.Object.Destroy(_previewDiceObj);
                _previewDiceObj = null;
            }

            ClearActiveDice();

            int count = (mode == RollMode.AdvantageTwoDice) ? 2 : 1;
            List<DiceController> currentRollControllers = new List<DiceController>();

            Vector3 baseSpawn = _settings != null ? _settings.SpawnPosition : new Vector3(0f, 3.5f, -0.5f);
            Vector3 spread = _settings != null ? _settings.SpawnSpread : new Vector3(1.2f, 0.2f, 0f);

            GameObject prefabToSpawn = GetPrefab(_currentDiceType);
            if (prefabToSpawn == null)
            {
                _isRolling = false;
                return new List<int> { 10 };
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = baseSpawn + (i - (count - 1) * 0.5f) * spread;
                Quaternion spawnRot = UnityEngine.Random.rotation;

                GameObject diceObj = UnityEngine.Object.Instantiate(prefabToSpawn, spawnPos, spawnRot);
                int diceLayer = LayerMask.NameToLayer("Dice");
                diceObj.layer = diceLayer != -1 ? diceLayer : 0;
                foreach (Transform child in diceObj.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = diceObj.layer;
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

            foreach (var die in currentRollControllers)
            {
                die.ThrowDice();
            }

            float maxTime = _settings != null ? _settings.MaxRollTimeoutSeconds : 4.5f;
            float startTime = Time.time;

            while (Time.time - startTime < maxTime)
            {
                bool allSleeping = true;
                foreach (var die in currentRollControllers)
                {
                    if (!die.IsSleeping())
                    {
                        allSleeping = false;
                        break;
                    }
                }

                if (allSleeping && (Time.time - startTime > 0.6f))
                {
                    break;
                }

                await Task.Yield();
            }

            List<int> results = new List<int>();
            foreach (var die in currentRollControllers)
            {
                results.Add(die.GetUpwardValue());
            }

            _isRolling = false;
            return results;
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
