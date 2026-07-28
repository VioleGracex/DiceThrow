using UnityEngine;

namespace BG3DiceSystem.Core.Interfaces
{
    public interface IEffectsService
    {
        void PlayDiceImpact(Vector3 position, float force);
        void PlaySuccessGlow();
        void PlayFailureFlash();
        void PlayCriticalSuccessExplosion(Vector3 position);
        void TriggerCameraShake(float intensity = 1f);
        void SetCameraZoom(bool zoomedIn);
    }
}
