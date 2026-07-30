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
        public event Action OnRollRequested;
        #endregion

        #region Private Fields
        private readonly Dictionary<DiceType, GameObject> _dicePrefabs;
        private readonly DiceSettingsSO _settings;
        private readonly List<DiceController> _spawnedDice = new List<DiceController>();
        private GameObject _previewDiceObj;
        private GameObject _previewDiceObjB;
        private DiceType _currentDiceType = DiceType.D20;
        private RollMode _currentRollMode = RollMode.SingleDie;
        private bool _isRolling;
        #endregion

        private void HandleDiceClicked()
        {
            if (!_isRolling)
            {
                Debug.Log("[DiceService] 3D Dice clicked by mouse! Requesting roll execution...");
                OnRollRequested?.Invoke();
            }
        }

        #region Properties
        public bool IsRolling => _isRolling;

        public DiceType CurrentDiceType
        {
            get => _currentDiceType;
            set
            {
                Debug.Log($"[DiceService] CurrentDiceType set to: {value}");
                _currentDiceType = value;
                OnDiceTypeChanged?.Invoke(_currentDiceType);
                SpawnPreviewDice(_currentDiceType, _currentRollMode);
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
        public void SpawnPreviewDice(DiceType type, RollMode mode = RollMode.SingleDie)
        {
            if (_isRolling) return;

            _currentDiceType = type;
            _currentRollMode = mode;

            // Instantly clear any existing rolled dice and previous preview dice
            ClearActiveDice();

            GameObject prefabToSpawn = GetPrefab(type);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[DiceService] Prefab for {type} not found!");
                return;
            }

            Vector3 originalScale = prefabToSpawn.transform.localScale;
            Vector3 centerPos = new Vector3(1000f, 1000f, 0f);

            if (mode == RollMode.SingleDie)
            {
                _previewDiceObj = CreatePreviewInstance(prefabToSpawn, centerPos);
                if (_previewDiceObj != null)
                {
                    _previewDiceObj.transform.localScale = Vector3.zero;
                    _previewDiceObj.transform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack);
                }
            }
            else // AdvantageTwoDice
            {
                Vector3 leftPos = centerPos + new Vector3(-0.28f, 0f, 0f);
                Vector3 rightPos = centerPos + new Vector3(0.28f, 0f, 0f);

                _previewDiceObj = CreatePreviewInstance(prefabToSpawn, leftPos);
                if (_previewDiceObj != null)
                {
                    _previewDiceObj.transform.localScale = Vector3.zero;
                    _previewDiceObj.transform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack);
                }

                _previewDiceObjB = CreatePreviewInstance(prefabToSpawn, rightPos);
                if (_previewDiceObjB != null)
                {
                    _previewDiceObjB.transform.localScale = Vector3.zero;
                    _previewDiceObjB.transform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack);
                }
            }
        }

        private GameObject CreatePreviewInstance(GameObject prefabToSpawn, Vector3 position)
        {
            Quaternion floatRot = Quaternion.Euler(0f, 0f, 0f);
            GameObject newDie = UnityEngine.Object.Instantiate(prefabToSpawn, position, floatRot);
            newDie.transform.localScale = prefabToSpawn.transform.localScale;

            int diceLayer = LayerMask.NameToLayer("Dice");
            int targetLayer = diceLayer != -1 ? diceLayer : 0;
            newDie.layer = targetLayer;
            foreach (Transform child in newDie.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = targetLayer;
            }

            Rigidbody rb = newDie.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            DiceController controller = newDie.GetComponent<DiceController>();
            if (controller == null) controller = newDie.AddComponent<DiceController>();

            if (controller != null)
            {
                newDie.transform.rotation = controller.CalculateFacingRotation(1, -Vector3.forward);
                controller.OnDiceClicked -= HandleDiceClicked;
                controller.OnDiceClicked += HandleDiceClicked;
            }

            return newDie;
        }
        #endregion

        #region Roll Execution Operations (BG3 Authentic Unity Physics Roll & Dot-Product Detection)
        public async Task<List<int>> RollDiceAsync(RollMode mode)
        {
            if (_isRolling) return new List<int>();
            _isRolling = true;
            _currentRollMode = mode;

            ClearActiveDice();

            int count = (mode == RollMode.AdvantageTwoDice) ? 2 : 1;
            List<DiceController> currentRollControllers = new List<DiceController>();
            List<int> results = new List<int>();

            Vector3 centerPos = new Vector3(1000f, 1000f, 0f);
            Vector3 spread = new Vector3(0.96f, 0f, 0f);

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
                diceObj.transform.localScale = prefabToSpawn.transform.localScale;

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
                controller.OnDiceClicked -= HandleDiceClicked;
                controller.OnDiceClicked += HandleDiceClicked;

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

            // 2. Wait until Rigidbody is nearly at rest, then start blending toward final face
            float maxRollTimeout = 4.0f;
            float startTime = Time.time;
            bool correctionStarted = false;
            Vector3 cameraDir = -Vector3.forward;

            float earlyCorrectVelSq  = 0.04f;
            float earlyCorrectAngSq  = 0.09f;

            while (Time.time - startTime < maxRollTimeout)
            {
                bool allSettled = true;
                bool allSlow    = true;

                foreach (var die in currentRollControllers)
                {
                    if (die == null) continue;
                    var rb = die.RigidBody;
                    if (rb == null || rb.isKinematic) continue;

                    float linSq = rb.linearVelocity.sqrMagnitude;
                    float angSq = rb.angularVelocity.sqrMagnitude;

                    if (!die.IsSleeping()) allSettled = false;
                    if (linSq > earlyCorrectVelSq || angSq > earlyCorrectAngSq) allSlow = false;
                }

                if (allSlow && !correctionStarted && Time.time - startTime > 0.3f)
                {
                    correctionStarted = true;
                    ProcessDiceSettling(currentRollControllers, mode, centerPos, cameraDir, results);
                    break;
                }

                if (allSettled && Time.time - startTime > 0.4f)
                    break;

                await Task.Delay(20);
            }

            if (!correctionStarted)
            {
                ProcessDiceSettling(currentRollControllers, mode, centerPos, cameraDir, results);
            }

            _isRolling = false;

            _ = RespawnPreviewAfterDelay(2.5f);

            return results;
        }

        private void ProcessDiceSettling(List<DiceController> currentRollControllers, RollMode mode, Vector3 centerPos, Vector3 cameraDir, List<int> results)
        {
            GameObject prefabToSpawn = GetPrefab(_currentDiceType);
            Vector3 originalScale = prefabToSpawn != null ? prefabToSpawn.transform.localScale : Vector3.one;

            if (mode == RollMode.AdvantageTwoDice && currentRollControllers.Count >= 2)
            {
                var dieA = currentRollControllers[0];
                var dieB = currentRollControllers[1];
                if (dieA != null && dieB != null)
                {
                    if (dieA.RigidBody != null) dieA.RigidBody.isKinematic = true;
                    if (dieB.RigidBody != null) dieB.RigidBody.isKinematic = true;

                    int valA = dieA.GetUpwardValue();
                    int valB = dieB.GetUpwardValue();
                    results.Add(valA);
                    results.Add(valB);

                    int winnerIndex = valA >= valB ? 0 : 1;
                    int loserIndex = valA >= valB ? 1 : 0;

                    var winnerDie = currentRollControllers[winnerIndex];
                    var loserDie = currentRollControllers[loserIndex];

                    int winnerVal = (winnerIndex == 0) ? valA : valB;
                    Quaternion winnerRot = winnerDie.CalculateFacingRotation(winnerVal, cameraDir);

                    // BG3 Advantage Stomp: Lower value die squashes & shrinks to scale 0
                    loserDie.transform.DOKill();
                    loserDie.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack);

                    // Higher value die scales UP bigger (1.35x of initial prefab scale) and stomps center position!
                    winnerDie.transform.DOKill();
                    winnerDie.transform.DOScale(originalScale * 1.35f, 0.45f).SetEase(Ease.OutBack);
                    winnerDie.transform.DOMove(centerPos, 0.45f).SetEase(Ease.OutQuad);
                    winnerDie.transform.DORotateQuaternion(winnerRot, 0.45f).SetEase(Ease.OutQuad);
                    return;
                }
            }

            for (int i = 0; i < currentRollControllers.Count; i++)
            {
                var die = currentRollControllers[i];
                if (die == null) continue;

                if (die.RigidBody != null) die.RigidBody.isKinematic = true;

                int rollVal = die.GetUpwardValue();
                results.Add(rollVal);

                Vector3 targetMiddlePos = centerPos + (i - (currentRollControllers.Count - 1) * 0.5f) * new Vector3(1.0f, 0f, 0f);
                Quaternion targetRot = die.CalculateFacingRotation(rollVal, cameraDir);

                die.transform.DOKill();
                die.transform.DOMove(targetMiddlePos, 0.8f, Ease.OutSine);
                die.transform.DORotateQuaternion(targetRot, 0.7f, Ease.OutSine);
                die.transform.DOScale(originalScale, 0.4f);
            }
        }

        private async Task RespawnPreviewAfterDelay(float delaySeconds)
        {
            await Task.Delay((int)(delaySeconds * 1000));
            if (!_isRolling && _spawnedDice.Count == 0)
            {
                SpawnPreviewDice(_currentDiceType, _currentRollMode);
            }
        }

        public void ClearActiveDice()
        {
            _isRolling = false;
            if (_previewDiceObj != null)
            {
                _previewDiceObj.SetActive(false);
                UnityEngine.Object.Destroy(_previewDiceObj);
                _previewDiceObj = null;
            }
            if (_previewDiceObjB != null)
            {
                _previewDiceObjB.SetActive(false);
                UnityEngine.Object.Destroy(_previewDiceObjB);
                _previewDiceObjB = null;
            }
            foreach (var die in _spawnedDice)
            {
                if (die != null && die.gameObject != null)
                {
                    die.gameObject.SetActive(false);
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
