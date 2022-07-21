using Content.Server.Explosion.EntitySystems;
using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
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
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(HandleEmitSoundOnLand);
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(HandleEmitSoundOnUseInHand);
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(HandleEmitSoundOnThrown);
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(HandleEmitSoundOnActivateInWorld);
            SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
            SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);
        }

        private void HandleEmitSoundOnTrigger(EntityUid uid, EmitSoundOnTriggerComponent component, TriggerEvent args)
        {
            TryEmitSound(component);
            args.Handled = true;
        }

        private void HandleEmitSoundOnLand(EntityUid eUI, BaseEmitSoundComponent component, LandEvent arg)
        {
            if (!TryComp<TransformComponent>(eUI, out var xform) ||
                !_mapManager.TryGetGrid(xform.GridUid, out var grid)) return;

            var tile = grid.GetTileRef(xform.Coordinates);

            if (tile.IsSpace(_tileDefMan)) return;

            TryEmitSound(component);
        }

        private void HandleEmitSoundOnUseInHand(EntityUid eUI, EmitSoundOnUseComponent component, UseInHandEvent arg)
        {
            // Intentionally not checking whether the interaction has already been handled.
            TryEmitSound(component);

            if (component.Handle)
                arg.Handled = true;
        }

        private void HandleEmitSoundOnThrown(EntityUid eUI, BaseEmitSoundComponent component, ThrownEvent arg)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnActivateInWorld(EntityUid eUI, EmitSoundOnActivateComponent component, ActivateInWorldEvent arg)
        {
            // Intentionally not checking whether the interaction has already been handled.
            TryEmitSound(component);

            if (component.Handle)
                arg.Handled = true;
        }

        private void HandleEmitSoundOnUIOpen(EntityUid eUI, BaseEmitSoundComponent component, AfterActivatableUIOpenEvent arg)
        {
            TryEmitSound(component);
        }

        private void TryEmitSound(BaseEmitSoundComponent component)
        {
            var audioParams = component.AudioParams.WithPitchScale((float) _random.NextGaussian(1, component.PitchVariation));
            SoundSystem.Play(component.Sound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, audioParams);
        }
    }
}

