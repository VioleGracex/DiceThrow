using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BG3DiceSystem.Core.Interfaces;
using BG3DiceSystem.Core.Services;
using BG3DiceSystem.Gameplay.Dice;
using BG3DiceSystem.Gameplay.Roll;
using BG3DiceSystem.Gameplay.Skills;
using BG3DiceSystem.Audio;
using BG3DiceSystem.Effects;
using BG3DiceSystem.Testing;

namespace BG3DiceSystem.Core.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {
        #region Inspector Fields - Scriptable Objects
        [Header("Scriptable Objects")]
        public List<SkillCheckSO> SkillChecks = new List<SkillCheckSO>();
        public DiceSettingsSO DiceSettings;
        public RollSettingsSO RollSettings;
        public AudioSettingsSO AudioSettings;
        #endregion

        #region Inspector Fields - Prefabs
        [Header("Dice Prefabs")]
        public GameObject PrefabD4;
        public GameObject PrefabD6;
        public GameObject PrefabD8;
        public GameObject PrefabD10;
        public GameObject PrefabD12;
        public GameObject PrefabD20;
        #endregion

        #region Inspector Fields - Scene References
        [Header("Scene References")]
        public AudioSource GlobalAudioSource;
        public EffectsController EffectsControllerRef;
        #endregion

        #region Zenject InstallBindings
        public override void InstallBindings()
        {
            // Bind Scriptable Objects
            Container.BindInstance(SkillChecks).AsSingle();
            Container.BindInstance(DiceSettings).AsSingle();
            Container.BindInstance(RollSettings).AsSingle();
            Container.BindInstance(AudioSettings).AsSingle();

            // Build and Bind Prefab Dictionary
            Dictionary<DiceType, GameObject> prefabs = new Dictionary<DiceType, GameObject>();
            if (PrefabD4 != null) prefabs[DiceType.D4] = PrefabD4;
            if (PrefabD6 != null) prefabs[DiceType.D6] = PrefabD6;
            if (PrefabD8 != null) prefabs[DiceType.D8] = PrefabD8;
            if (PrefabD10 != null) prefabs[DiceType.D10] = PrefabD10;
            if (PrefabD12 != null) prefabs[DiceType.D12] = PrefabD12;
            if (PrefabD20 != null) prefabs[DiceType.D20] = PrefabD20;

            Container.BindInstance(prefabs).AsSingle();

            // Bind Scene Components
            if (GlobalAudioSource != null) Container.BindInstance(GlobalAudioSource).AsSingle();
            if (EffectsControllerRef != null) Container.BindInstance(EffectsControllerRef).AsSingle();

            // Bind Core Gameplay Services
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<ISkillService>().To<SkillService>().AsSingle();
            Container.Bind<IDiceService>().To<DiceService>().AsSingle();
            Container.Bind<IEffectsService>().To<EffectsService>().AsSingle();
            Container.Bind<IAudioService>().To<AudioService>().AsSingle();
            Container.Bind<IRollService>().To<RollService>().AsSingle();

            // Bind Testing Services
            Container.Bind<AutoPlayTestRunner>().FromComponentInHierarchy().AsSingle();
        }
        #endregion
    }
}
