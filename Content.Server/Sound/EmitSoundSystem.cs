using Content.Server.Explosion.EntitySystems;
using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Sound
{
    /// <summary>
    /// Will play a sound on various events if the affected entity has a component derived from BaseEmitSoundComponent
    /// </summary>
    [UsedImplicitly]
    public sealed class EmitSoundSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(HandleEmitSoundOnLand);
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(HandleEmitSoundOnUseInHand);
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(HandleEmitSoundOnThrown);
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(HandleEmitSoundOnActivateInWorld);
            SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
        }

        private void HandleEmitSoundOnTrigger(EntityUid uid, EmitSoundOnTriggerComponent component, TriggerEvent args)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnLand(EntityUid eUI, BaseEmitSoundComponent component, LandEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnUseInHand(EntityUid eUI, BaseEmitSoundComponent component, UseInHandEvent arg)
        {
            // Intentionally not handling interaction. This component is an easy way to add sounds in addition to other behavior.
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnThrown(EntityUid eUI, BaseEmitSoundComponent component, ThrownEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnActivateInWorld(EntityUid eUI, BaseEmitSoundComponent component, ActivateInWorldEvent arg)
        {
            // Intentionally not handling interaction. This component is an easy way to add sounds in addition to other behavior.
            TryEmitSound(component);
        }

        private void TryEmitSound(BaseEmitSoundComponent component)
        {
            var audioParams = component.AudioParams.WithPitchScale((float) _random.NextGaussian(1, component.PitchVariation));
            SoundSystem.Play(Filter.Pvs(component.Owner, entityManager: EntityManager), component.Sound.GetSound(), component.Owner, audioParams);
        }
    }
}

