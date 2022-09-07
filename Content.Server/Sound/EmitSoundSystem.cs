using Content.Server.Explosion.EntitySystems;
using Content.Server.Interaction.Components;
using Content.Server.Sound.Components;
using Content.Server.Throwing;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Throwing;
using JetBrains.Annotations;
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
        [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        /// <inheritdoc />

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var soundSpammer in EntityQuery<SpamEmitSoundComponent>())
            {
                if (!soundSpammer.Enabled)
                    continue;

                soundSpammer.Accumulator += frameTime;
                if (soundSpammer.Accumulator < soundSpammer.RollInterval)
                {
                    continue;
                }
                soundSpammer.Accumulator -= soundSpammer.RollInterval;

                if (_random.Prob(soundSpammer.PlayChance))
                {
                    if (soundSpammer.PopUp != null)
                        _popupSystem.PopupEntity(Loc.GetString(soundSpammer.PopUp), soundSpammer.Owner, Filter.Pvs(soundSpammer.Owner));
                    TryEmitSound(soundSpammer);
                }
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnSpawnComponent, ComponentInit>(HandleEmitSpawnOnInit);
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(HandleEmitSoundOnLand);
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(HandleEmitSoundOnUseInHand);
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(HandleEmitSoundOnThrown);
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(HandleEmitSoundOnActivateInWorld);
            SubscribeLocalEvent<EmitSoundOnTriggerComponent, TriggerEvent>(HandleEmitSoundOnTrigger);
            SubscribeLocalEvent<EmitSoundOnUIOpenComponent, AfterActivatableUIOpenEvent>(HandleEmitSoundOnUIOpen);
            SubscribeLocalEvent<EmitSoundOnPickupComponent, GotEquippedHandEvent>(HandleEmitSoundOnPickup);
            SubscribeLocalEvent<EmitSoundOnDropComponent, DroppedEvent>(HandleEmitSoundOnDrop);
        }

        private void HandleEmitSpawnOnInit(EntityUid uid, EmitSoundOnSpawnComponent component, ComponentInit args)
        {
            TryEmitSound(component);
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

        private void HandleEmitSoundOnPickup(EntityUid uid, EmitSoundOnPickupComponent component, GotEquippedHandEvent args)
        {
            TryEmitSound(component);
        }

        private void HandleEmitSoundOnDrop(EntityUid uid, EmitSoundOnDropComponent component, DroppedEvent args)
        {
            TryEmitSound(component);
        }

        private void TryEmitSound(BaseEmitSoundComponent component)
        {
            if (component.Sound == null)
                return;
            _audioSystem.PlayPvs(component.Sound, component.Owner, component.Sound.Params.AddVolume(-2f));
        }
    }
}

