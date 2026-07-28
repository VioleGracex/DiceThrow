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

        private DiceSettingsSO _settings;
        private float _sleepThreshold = 0.05f;

        private void Awake()
        {
            if (RigidBody == null) RigidBody = GetComponent<Rigidbody>();
            if (ResultDetector == null) ResultDetector = GetComponent<DiceResultDetector>();

            if (RigidBody != null)
            {
                RigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        private void Update()
        {
            // Safety catch: prevent die from tunneling below floor into space
            if (transform.position.y < -1.5f)
            {
                Debug.LogWarning($"[DiceController] Die fell below floor threshold! Resetting position: {transform.position}");
                transform.position = new Vector3(transform.position.x * 0.5f, 0.5f, transform.position.z * 0.5f);
                if (RigidBody != null)
                {
                    RigidBody.linearVelocity = Vector3.zero;
                    RigidBody.angularVelocity = Vector3.zero;
                }
            }
        }

        public void Initialize(DiceSettingsSO settings)
        {
            _settings = settings;
            if (_settings != null)
            {
                _sleepThreshold = _settings.SleepVelocityThreshold;
            }
        }

        public void ThrowDice()
        {
            if (RigidBody == null) return;

            RigidBody.isKinematic = false;
            RigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            float minForce = _settings != null ? _settings.MinThrowForce : 7f;
            float maxForce = _settings != null ? _settings.MaxThrowForce : 11f;
            float minTorque = _settings != null ? _settings.MinTorque : 15f;
            float maxTorque = _settings != null ? _settings.MaxTorque : 30f;

            // Random force vector pointing downwards and slightly forward/side
            Vector3 forceDir = new Vector3(
                UnityEngine.Random.Range(-0.4f, 0.4f),
                UnityEngine.Random.Range(-1.0f, -0.6f),
                UnityEngine.Random.Range(-0.3f, 0.3f)
            ).normalized;

            float forceMag = UnityEngine.Random.Range(minForce, maxForce);
            RigidBody.AddForce(forceDir * forceMag, ForceMode.Impulse);

            // Random torque vector for natural tumbling
            Vector3 torque = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized * UnityEngine.Random.Range(minTorque, maxTorque);

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

        private void OnCollisionEnter(Collision collision)
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > 0.5f)
            {
                OnImpact?.Invoke(transform, impactForce);
            }
        }
    }
}
