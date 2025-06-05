using Content.Shared.Atmos;
using Content.Shared.Camera;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<HandsComponent, GetEyeOffsetRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, GetEyePvsScaleRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshMovementSpeedModifiersEvent>(RelayEvent);

        // By-ref events.
        SubscribeLocalEvent<HandsComponent, ExtinguishEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, ProjectileReflectAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, HitScanReflectAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, WieldAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, UnwieldAttemptEvent>(RefRelayEvent);
    }

    private void RelayEvent<T>(Entity<HandsComponent> entity, ref T args) where T : EntityEventArgs
    {
        CoreRelayEvent(entity, ref args);
    }

    private void RefRelayEvent<T>(Entity<HandsComponent> entity, ref T args)
    {
        var ev = CoreRelayEvent(entity, ref args);
        args = ev.Args;
    }

    private HeldRelayedEvent<T> CoreRelayEvent<T>(Entity<HandsComponent> entity, ref T args)
    {
        var ev = new HeldRelayedEvent<T>(args);

        foreach (var held in EnumerateHeld(entity, entity.Comp))
        {
            RaiseLocalEvent(held, ref ev);
        }

        return ev;
    }
}
