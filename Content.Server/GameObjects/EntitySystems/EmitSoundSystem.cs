using Content.Server.GameObjects.Components.Sound;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class EmitSoundSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>((eUI, comp, arg) => PlaySoundBasedOnMode(eUI, comp));
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>((eUI, comp, arg) => PlaySoundBasedOnMode(eUI, comp));
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>((eUI, comp, arg) => PlaySoundBasedOnMode(eUI, comp));
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>((eUI, comp, args) => PlaySoundBasedOnMode(eUI, comp));
        }

        private void PlaySoundBasedOnMode(EntityUid uid, BaseEmitSoundComponent component)
        {
            if (component == null)
            {
                return;
            }
#pragma warning disable CS8604 // Possible null reference argument.
            switch (component._emitSoundMode)
            {
                case EmitSoundMode.Single:
                    PlaySingleSound(component._soundName, component);
                    break;
                case EmitSoundMode.RandomFromCollection:
                    PlayRandomSoundFromCollection(component);
                    break;
                case EmitSoundMode.Auto:
                    if (!string.IsNullOrEmpty(component._soundCollectionName))
                    {
                        PlayRandomSoundFromCollection(component);
                        break;
                    }
                    PlaySingleSound(component._soundName, component);
                    break;
                default:
                    break;
            }
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private void PlayRandomSoundFromCollection(BaseEmitSoundComponent component)
        {
            if (!string.IsNullOrWhiteSpace(component._soundCollectionName))
            {
                var file = SelectRandomSoundFromSoundCollection(component._soundCollectionName);
                PlaySingleSound(file, component);
            }
        }

        private string SelectRandomSoundFromSoundCollection(string soundCollectionName)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
            return _random.Pick(soundCollection.PickFiles);
        }

        private static void PlaySingleSound(string soundName, BaseEmitSoundComponent component)
        {
            if (string.IsNullOrWhiteSpace(soundName))
            {
                return;
            }

            if (component._pitchVariation > 0.0)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), soundName, component.Owner,
                                 AudioHelpers.WithVariation(component._pitchVariation).WithVolume(-2f));
            }
            else if (component._semitoneVariation > 0)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), soundName, component.Owner,
                                 AudioHelpers.WithSemitoneVariation(component._semitoneVariation).WithVolume(-2f));
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), soundName, component.Owner,
                                 AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}

