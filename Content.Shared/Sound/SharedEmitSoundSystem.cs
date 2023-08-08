using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Sound.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Sound;

/// <summary>
/// Will play a sound on various events if the affected entity has a component derived from BaseEmitSoundComponent
/// </summary>
[UsedImplicitly]
public abstract class SharedEmitSoundSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitSoundOnSpawnComponent, ComponentInit>(OnEmitSpawnOnInit);
        SubscribeLocalEvent<EmitSoundOnLandComponent, LandEvent>(OnEmitSoundOnLand);
        SubscribeLocalEvent<EmitSoundOnUseComponent, UseInHandEvent>(OnEmitSoundOnUseInHand);
        SubscribeLocalEvent<EmitSoundOnThrowComponent, ThrownEvent>(OnEmitSoundOnThrown);
        SubscribeLocalEvent<EmitSoundOnActivateComponent, ActivateInWorldEvent>(OnEmitSoundOnActivateInWorld);
        SubscribeLocalEvent<EmitSoundOnPickupComponent, GotEquippedHandEvent>(OnEmitSoundOnPickup);
        SubscribeLocalEvent<EmitSoundOnDropComponent, DroppedEvent>(OnEmitSoundOnDrop);

        SubscribeLocalEvent<EmitSoundOnCollideComponent, EntityUnpausedEvent>(OnEmitSoundUnpaused);
        SubscribeLocalEvent<EmitSoundOnCollideComponent, StartCollideEvent>(OnEmitSoundOnCollide);
    }

    private void OnEmitSpawnOnInit(EntityUid uid, EmitSoundOnSpawnComponent component, ComponentInit args)
    {
        TryEmitSound(uid, component, predict: false);
    }

    private void OnEmitSoundOnLand(EntityUid uid, BaseEmitSoundComponent component, ref LandEvent args)
    {
        if (!args.PlaySound ||
            !TryComp<TransformComponent>(uid, out var xform) ||
            !_mapManager.TryGetGrid(xform.GridUid, out var grid))
        {
            return;
        }

        var tile = grid.GetTileRef(xform.Coordinates);

        // Handle maps being grids (we'll still emit the sound).
        if (xform.GridUid != xform.MapUid && tile.IsSpace(_tileDefMan))
            return;

        // hand throwing not predicted sadly
        TryEmitSound(uid, component, args.User, false);
    }

    private void OnEmitSoundOnUseInHand(EntityUid uid, EmitSoundOnUseComponent component, UseInHandEvent args)
    {
        // Intentionally not checking whether the interaction has already been handled.
        TryEmitSound(uid, component, args.User);

        if (component.Handle)
            args.Handled = true;
    }

    private void OnEmitSoundOnThrown(EntityUid uid, BaseEmitSoundComponent component, ThrownEvent args)
    {
        TryEmitSound(uid, component, args.User, false);
    }

    private void OnEmitSoundOnActivateInWorld(EntityUid uid, EmitSoundOnActivateComponent component, ActivateInWorldEvent args)
    {
        // Intentionally not checking whether the interaction has already been handled.
        TryEmitSound(uid, component, args.User);

        if (component.Handle)
            args.Handled = true;
    }

    private void OnEmitSoundOnPickup(EntityUid uid, EmitSoundOnPickupComponent component, GotEquippedHandEvent args)
    {
        TryEmitSound(uid, component, args.User);
    }

    private void OnEmitSoundOnDrop(EntityUid uid, EmitSoundOnDropComponent component, DroppedEvent args)
    {
        TryEmitSound(uid, component, args.User);
    }

    protected void TryEmitSound(EntityUid uid, BaseEmitSoundComponent component, EntityUid? user=null, bool predict=true)
    {
        if (component.Sound == null)
            return;

        if (predict)
        {
            _audioSystem.PlayPredicted(component.Sound, uid, user);
        }
        else if (_netMan.IsServer)
        {
            // don't predict sounds that client couldn't have played already
            _audioSystem.PlayPvs(component.Sound, uid);
        }
    }

    private void OnEmitSoundUnpaused(EntityUid uid, EmitSoundOnCollideComponent component, ref EntityUnpausedEvent args)
    {
        component.NextSound += args.PausedTime;
    }

    private void OnEmitSoundOnCollide(EntityUid uid, EmitSoundOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OurFixture.Hard ||
            !args.OtherFixture.Hard ||
            !TryComp<PhysicsComponent>(uid, out var physics) ||
            physics.LinearVelocity.Length() < component.MinimumVelocity ||
            _timing.CurTime < component.NextSound ||
            MetaData(uid).EntityPaused)
        {
            return;
        }

        const float MaxVolumeVelocity = 10f;
        const float MinVolume = -10f;
        const float MaxVolume = 2f;

        var fraction = MathF.Min(1f, (physics.LinearVelocity.Length() - component.MinimumVelocity) / MaxVolumeVelocity);
        var volume = MinVolume + (MaxVolume - MinVolume) * fraction;
        component.NextSound = _timing.CurTime + EmitSoundOnCollideComponent.CollideCooldown;

        if (_netMan.IsServer)
            _audioSystem.PlayPvs(component.Sound, uid, AudioParams.Default.WithVolume(volume));
    }
}
