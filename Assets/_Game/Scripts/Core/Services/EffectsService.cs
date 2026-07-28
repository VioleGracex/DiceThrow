using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Effects;

namespace BG3DiceSystem.Core.Services
{
    public class EffectsService : IEffectsService
    {
        private readonly EffectsController _effectsController;

        public EffectsService(EffectsController effectsController)
        {
            _effectsController = effectsController;
        }

        public void PlayDiceImpact(Vector3 position, float force)
        {
            _effectsController?.PlayImpactEffect(position, force);
        }

        public void PlaySuccessGlow()
        {
            _effectsController?.PlaySuccessGlow();
        }

        public void PlayFailureFlash()
        {
            _effectsController?.PlayFailureFlash();
        }

        public void PlayCriticalSuccessExplosion(Vector3 position)
        {
            _effectsController?.PlayCriticalSuccessExplosion(position);
        }

        public void TriggerCameraShake(float intensity = 1)
        {
            _effectsController?.TriggerCameraShake(intensity);
        }

        public void SetCameraZoom(bool zoomedIn)
        {
            _effectsController?.SetCameraZoom(zoomedIn);
        }
    }
}
