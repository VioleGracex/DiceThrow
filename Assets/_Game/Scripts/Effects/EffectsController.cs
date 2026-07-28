using UnityEngine;
using System.Collections;
using BG3DiceSystem.Core.Utilities.Tweening;

namespace BG3DiceSystem.Effects
{
    public class EffectsController : MonoBehaviour
    {
        [Header("Particle Systems")]
        public ParticleSystem ImpactDustParticles;
        public ParticleSystem SuccessGlowParticles;
        public ParticleSystem CriticalExplosionParticles;

        [Header("UI / Screen Effects")]
        public CanvasGroup ScreenFlashCanvasGroup;
        public UnityEngine.UI.Image ScreenFlashImage;

        [Header("Camera Control")]
        public Camera MainCamera;
        public Camera OverlayDiceCamera;

        private Vector3 _mainCamOriginalPos;
        private float _mainCamOriginalFOV;

        private void Awake()
        {
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera != null)
            {
                _mainCamOriginalPos = MainCamera.transform.position;
                _mainCamOriginalFOV = MainCamera.fieldOfView;
            }
        }

        public void PlayImpactEffect(Vector3 position, float force)
        {
            if (ImpactDustParticles != null)
            {
                SetParticleLayer(ImpactDustParticles.gameObject);
                ImpactDustParticles.transform.position = position;
                var main = ImpactDustParticles.main;
                main.startSizeMultiplier = Mathf.Clamp(force * 0.4f, 1.5f, 4f);
                ImpactDustParticles.Play();
            }
        }

        public void PlaySuccessGlow()
        {
            if (SuccessGlowParticles != null)
            {
                SetParticleLayer(SuccessGlowParticles.gameObject);
                SuccessGlowParticles.transform.position = new Vector3(1000f, 1000f, 0f);
                SuccessGlowParticles.Play();
            }
            FlashScreen(new Color(0.2f, 0.85f, 0.35f, 0.35f), 0.6f);
        }

        public void PlayFailureFlash()
        {
            FlashScreen(new Color(0.85f, 0.2f, 0.2f, 0.4f), 0.6f);
        }

        public void PlayCriticalSuccessExplosion(Vector3 position)
        {
            if (CriticalExplosionParticles != null)
            {
                SetParticleLayer(CriticalExplosionParticles.gameObject);
                CriticalExplosionParticles.transform.position = position;
                CriticalExplosionParticles.Play();
            }
            FlashScreen(new Color(1f, 0.84f, 0f, 0.6f), 0.9f);
            TriggerCameraShake(1.8f);
        }

        private void SetParticleLayer(GameObject target)
        {
            if (target == null) return;
            int diceLayer = LayerMask.NameToLayer("Dice");
            int layer = diceLayer != -1 ? diceLayer : 0;
            target.layer = layer;
            foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        public void FlashScreen(Color flashColor, float duration)
        {
            if (ScreenFlashCanvasGroup != null && ScreenFlashImage != null)
            {
                ScreenFlashImage.color = flashColor;
                ScreenFlashCanvasGroup.alpha = 1f;
                ScreenFlashCanvasGroup.DOFade(0f, duration, Ease.OutQuad);
            }
        }

        public void TriggerCameraShake(float intensity = 1f)
        {
            if (MainCamera != null)
            {
                StartCoroutine(ShakeRoutine(MainCamera.transform, _mainCamOriginalPos, 0.35f, intensity * 0.15f));
            }
        }

        public void SetCameraZoom(bool zoomedIn)
        {
            // Camera position, FOV, and framing remain completely fixed throughout rolls
        }

        private IEnumerator ShakeRoutine(Transform target, Vector3 origin, float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 offset = Random.insideUnitSphere * magnitude * (1f - (elapsed / duration));
                offset.z = 0f; // keep depth steady
                target.position = origin + offset;
                yield return null;
            }
            target.position = origin;
        }

        private IEnumerator AnimateFOV(Camera cam, float targetFOV, float duration)
        {
            if (cam == null) yield break;
            float start = cam.fieldOfView;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cam.fieldOfView = Mathf.Lerp(start, targetFOV, elapsed / duration);
                yield return null;
            }
            cam.fieldOfView = targetFOV;
        }
    }
}
