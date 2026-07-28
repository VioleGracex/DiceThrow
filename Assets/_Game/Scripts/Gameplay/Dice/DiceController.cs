using System;
using UnityEngine;

namespace BG3DiceSystem.Gameplay.Dice
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiceController : MonoBehaviour
    {
        public event Action<Transform, float> OnImpact;

        [Header("Components")]
        public Rigidbody RigidBody;
        public DiceResultDetector ResultDetector;

        [Header("Roll Zone Boundary")]
        public Vector3 RollCenter = new Vector3(1000f, 1000f, 0f);
        public float RollZoneRadius = 1.0f;

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
                RigidBody.linearDamping = 2.5f;
                RigidBody.angularDamping = 2.5f;
                RigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
                RigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            PhysicsMaterial diceMaterial = new PhysicsMaterial("DiceRollMaterial")
            {
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                dynamicFriction = 0.9f,
                staticFriction = 0.9f,
                frictionCombine = PhysicsMaterialCombine.Maximum
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

            // Constrain die movement within small circular middle area
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
                    vel2D = Vector2.Reflect(vel2D, -dir) * 0.5f;
                    RigidBody.linearVelocity = new Vector3(vel2D.x, vel2D.y, 0f);
                }
            }
            else
            {
                // Gentle inward spring pulling towards center for natural swirl
                Vector2 springForce = -offset * 2.0f;
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

            float minForce = _settings != null ? _settings.MinThrowForce : 3f;
            float maxForce = _settings != null ? _settings.MaxThrowForce : 5f;
            float minTorque = _settings != null ? _settings.MinTorque : 10f;
            float maxTorque = _settings != null ? _settings.MaxTorque : 20f;

            // Random horizontal direction inside screen plane
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 forceDir = new Vector3(randomCircle.x, randomCircle.y, 0f);

            float forceMag = UnityEngine.Random.Range(minForce, maxForce);
            RigidBody.AddForce(forceDir * forceMag, ForceMode.Impulse);

            // Random 3D torque for natural horizontal tumbling and rolling
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
