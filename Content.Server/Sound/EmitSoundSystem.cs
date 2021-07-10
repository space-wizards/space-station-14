using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.Sound
{
    /// <summary>
    /// Will play a sound on various events if the affected entity has a component derived from BaseEmitSoundComponent
    /// </summary>
    [UsedImplicitly]
    public class EmitSoundSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>((eUI, comp, arg) => HandleEmitSoundOn(comp));
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>((eUI, comp, arg) => HandleEmitSoundOn(comp));
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>((eUI, comp, arg) => HandleEmitSoundOn(comp));
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>((eUI, comp, args) => HandleEmitSoundOn(comp));
        }

        private void HandleEmitSoundOn(BaseEmitSoundComponent component)
        {
            if (component.Sound.TryGetSound(out var soundName))
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), soundName, component.Owner, AudioHelpers.WithVariation(component.PitchVariation).WithVolume(-2f));
            }
            else
            {
                Logger.Warning($"{nameof(component)} Uid:{component.Owner.Uid} has no {nameof(component.Sound)} to play.");
            }
        }
    }
}

