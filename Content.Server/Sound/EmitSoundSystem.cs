using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Log;

namespace Content.Server.Sound
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
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>((eUI, comp, arg) => PlaySound(comp));
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>((eUI, comp, arg) => PlaySound(comp));
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>((eUI, comp, arg) => PlaySound(comp));
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>((eUI, comp, args) => PlaySound(comp));
        }

        private void PlaySound(BaseEmitSoundComponent component)
        {
            if (!string.IsNullOrWhiteSpace(component.SoundCollectionName))
            {
                PlayRandomSoundFromCollection(component);
            }
            else if (!string.IsNullOrWhiteSpace(component.SoundName))
            {
                PlaySingleSound(component.SoundName, component);
            }
            else
            {
                Logger.Warning($"{nameof(component)} Uid:{component.Owner.Uid} has neither {nameof(component.SoundCollectionName)} nor {nameof(component.SoundName)} to play.");
            }
        }

        private void PlayRandomSoundFromCollection(BaseEmitSoundComponent component)
        {
            var file = SelectRandomSoundFromSoundCollection(component.SoundCollectionName!);
            PlaySingleSound(file, component);
        }

        private string SelectRandomSoundFromSoundCollection(string soundCollectionName)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
            return _random.Pick(soundCollection.PickFiles);
        }

        private static void PlaySingleSound(string soundName, BaseEmitSoundComponent component)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), soundName, component.Owner,
                             AudioHelpers.WithVariation(component.PitchVariation).WithVolume(-2f));
        }
    }
}

