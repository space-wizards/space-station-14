using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Sound.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Sound
{
    /// <summary>
    /// Will play a sound on various events if the affected entity has a component derived from BaseEmitSoundComponent
    /// </summary>
    [UsedImplicitly]
    public abstract class SharedEmitSoundSystem : EntitySystem
    {
        [Dependency] private readonly INetManager _netMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
        [Dependency] protected readonly IRobustRandom Random = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] protected readonly SharedPopupSystem Popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmitSoundOnSpawnComponent, ComponentInit>(HandleEmitSpawnOnInit);
            SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(HandleEmitSoundOnLand);
            SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(HandleEmitSoundOnUseInHand);
            SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(HandleEmitSoundOnThrown);
            SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(HandleEmitSoundOnActivateInWorld);
            SubscribeLocalEvent<EmitSoundOnPickupComponent, GotEquippedHandEvent>(HandleEmitSoundOnPickup);
            SubscribeLocalEvent<EmitSoundOnDropComponent, DroppedEvent>(HandleEmitSoundOnDrop);
        }

        private void HandleEmitSpawnOnInit(EntityUid uid, EmitSoundOnSpawnComponent component, ComponentInit args)
        {
            TryEmitSound(component, predict: false);
        }

        private void HandleEmitSoundOnLand(EntityUid uid, BaseEmitSoundComponent component, LandEvent args)
        {
            if (!TryComp<TransformComponent>(uid, out var xform) ||
                !_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return;

            var tile = grid.GetTileRef(xform.Coordinates);

            // Handle maps being grids (we'll still emit the sound).
            if (xform.GridUid != xform.MapUid && tile.IsSpace(_tileDefMan))
                return;

            // hand throwing not predicted sadly
            TryEmitSound(component, args.User, false);
        }

        private void HandleEmitSoundOnUseInHand(EntityUid eUI, EmitSoundOnUseComponent component, UseInHandEvent args)
        {
            // Intentionally not checking whether the interaction has already been handled.
            TryEmitSound(component, args.User);

            if (component.Handle)
                args.Handled = true;
        }

        private void HandleEmitSoundOnThrown(EntityUid eUI, BaseEmitSoundComponent component, ThrownEvent args)
        {
            TryEmitSound(component, args.User, false);
        }

        private void HandleEmitSoundOnActivateInWorld(EntityUid eUI, EmitSoundOnActivateComponent component, ActivateInWorldEvent args)
        {
            // Intentionally not checking whether the interaction has already been handled.
            TryEmitSound(component, args.User);

            if (component.Handle)
                args.Handled = true;
        }

        private void HandleEmitSoundOnPickup(EntityUid uid, EmitSoundOnPickupComponent component, GotEquippedHandEvent args)
        {
            TryEmitSound(component, args.User);
        }

        private void HandleEmitSoundOnDrop(EntityUid uid, EmitSoundOnDropComponent component, DroppedEvent args)
        {
            TryEmitSound(component, args.User);
        }

        protected void TryEmitSound(BaseEmitSoundComponent component, EntityUid? user=null, bool predict=true)
        {
            if (component.Sound == null)
                return;

            if (predict)
            {
                _audioSystem.PlayPredicted(component.Sound, component.Owner, user, component.Sound.Params.AddVolume(-2f));
            }
            else if (_netMan.IsServer)
            {
                // don't predict sounds that client couldn't have played already
                _audioSystem.PlayPvs(component.Sound, component.Owner, component.Sound.Params.AddVolume(-2f));
            }
        }
    }
}

