using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BG3DiceSystem.Gameplay.Dice
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public event Action<Transform, float> OnImpact;
        public event Action OnDiceClicked;
        public event Action OnDiceSwiped;

        private Vector2 _pointerStartPos;
        private bool _isPointerActive;
        private const float SwipeThreshold = 12f;

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerStartPos = eventData.position;
            _isPointerActive = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isPointerActive)
            {
                float delta = Vector2.Distance(eventData.position, _pointerStartPos);
                if (delta >= SwipeThreshold)
                {
                    _isPointerActive = false;
                    Debug.Log($"[DiceController] Swipe gesture detected on die '{gameObject.name}'! (delta: {delta:F1}px)");
                    OnDiceSwiped?.Invoke();
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerActive = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isPointerActive || eventData.dragging) return;
            Debug.Log($"[DiceController] PointerClick detected on die '{gameObject.name}'!");
            OnDiceClicked?.Invoke();
        }

        [Header("Components")]
        public Rigidbody RigidBody;
        public DiceResultDetector ResultDetector;

        [Header("Roll Zone Boundary")]
        public Vector3 RollCenter = new Vector3(1000f, 1000f, 0f);
        public float RollZoneRadius = 1.2f;

        private DiceSettingsSO _settings;
        private float _sleepThreshold = 0.05f;

        private void Awake()
        {
            if (RigidBody == null) RigidBody = GetComponent<Rigidbody>();
            if (ResultDetector == null) ResultDetector = GetComponent<DiceResultDetector>();

            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (RigidBody != null)
            {
                RigidBody.useGravity = false;
                RigidBody.linearDamping = 0.8f;
                RigidBody.angularDamping = 0.6f;
                RigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
                RigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            PhysicsMaterial diceMaterial = new PhysicsMaterial("DiceRollMaterial")
            {
                bounciness = 0.2f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.4f,
                staticFriction = 0.4f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.material = diceMaterial;
            }
        }

        private void FixedUpdate()
        {
            if (RigidBody == null || RigidBody.isKinematic) return;

            // Keep die on surface plane Z = 0
            Vector3 pos = transform.position;
            pos.z = RollCenter.z;

            // Constrain die movement within circular middle area
            Vector2 offset = new Vector2(pos.x - RollCenter.x, pos.y - RollCenter.y);
            float dist = offset.magnitude;

            if (dist > RollZoneRadius)
            {
                Vector2 dir = offset.normalized;
                pos.x = RollCenter.x + dir.x * RollZoneRadius;
                pos.y = RollCenter.y + dir.y * RollZoneRadius;

                Vector3 vel = RigidBody.linearVelocity;
                Vector2 vel2D = new Vector2(vel.x, vel.y);
                if (Vector2.Dot(vel2D, dir) > 0)
                {
                    vel2D = Vector2.Reflect(vel2D, -dir) * 0.7f;
                    RigidBody.linearVelocity = new Vector3(vel2D.x, vel2D.y, 0f);
                }
            }
            else
            {
                // Gentle inward spring pulling towards center for natural swirl
                Vector2 springForce = -offset * 3.0f;
                RigidBody.AddForce(new Vector3(springForce.x, springForce.y, 0f), ForceMode.Acceleration);
            }

            transform.position = pos;
        }

        public void Initialize(DiceSettingsSO settings)
        {
            _settings = settings;
            if (_settings != null)
            {
                _sleepThreshold = _settings.SleepVelocityThreshold;
            }
            ConfigurePhysics();
        }

        public void ThrowDice()
        {
            if (RigidBody == null) return;

            RigidBody.isKinematic = false;
            ConfigurePhysics();

            float minForce = _settings != null ? Mathf.Max(_settings.MinThrowForce, 5f) : 5f;
            float maxForce = _settings != null ? Mathf.Max(_settings.MaxThrowForce, 9f) : 9f;
            float minTorque = _settings != null ? Mathf.Max(_settings.MinTorque, 25f) : 25f;
            float maxTorque = _settings != null ? Mathf.Max(_settings.MaxTorque, 45f) : 45f;

            // Random horizontal direction inside screen plane
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 forceDir = new Vector3(randomCircle.x, randomCircle.y, 0f);

            float forceMag = UnityEngine.Random.Range(minForce, maxForce);
            RigidBody.AddForce(forceDir * forceMag, ForceMode.Impulse);

            // High 3D torque for enthusiastic tumbling and spinning
            Vector3 torque = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(minTorque, maxTorque);
            RigidBody.AddTorque(torque, ForceMode.Impulse);
        }

        public bool IsSleeping()
        {
            if (RigidBody == null) return true;
            return RigidBody.isKinematic || (RigidBody.linearVelocity.sqrMagnitude < _sleepThreshold * _sleepThreshold && RigidBody.angularVelocity.sqrMagnitude < _sleepThreshold * _sleepThreshold);
        }

        public int GetUpwardValue()
        {
            if (ResultDetector != null)
            {
                return ResultDetector.GetUpwardFaceValue();
            }
            return 20;
        }

        public Quaternion CalculateFacingRotation(int faceValue, Vector3 cameraDir)
        {
            if (ResultDetector != null)
            {
                return ResultDetector.GetFacingRotation(faceValue, cameraDir);
            }
            return transform.rotation;
        }



        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > 0.5f)
            {
                OnImpact?.Invoke(transform, impactForce);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireSphere(RollCenter, RollZoneRadius);
        }
    }
}
