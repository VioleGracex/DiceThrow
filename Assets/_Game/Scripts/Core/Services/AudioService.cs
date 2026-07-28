using UnityEngine;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Audio;

namespace BG3DiceSystem.Core.Services
{
    public class AudioService : IAudioService
    {
        private readonly AudioSettingsSO _settings;
        private readonly AudioSource _audioSource;

        public AudioService(AudioSettingsSO settings, AudioSource audioSource)
        {
            _settings = settings;
            _audioSource = audioSource;
        }

        private void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }

        public void PlayButtonClick() => PlayOneShot(_settings?.ButtonClick, 0.7f);
        public void PlayDiceThrow() => PlayOneShot(_settings?.DiceThrow, 0.8f);
        public void PlayDiceBounce() => PlayOneShot(_settings?.DiceBounce, 0.6f);
        public void PlayHeavyLanding() => PlayOneShot(_settings?.HeavyLanding, 0.9f);
        public void PlaySuccess() => PlayOneShot(_settings?.Success, 1f);
        public void PlayFailure() => PlayOneShot(_settings?.Failure, 1f);
        public void PlayCriticalSuccess() => PlayOneShot(_settings?.CriticalSuccess, 1f);
        public void PlayCriticalFailure() => PlayOneShot(_settings?.CriticalFailure, 1f);
    }
}
