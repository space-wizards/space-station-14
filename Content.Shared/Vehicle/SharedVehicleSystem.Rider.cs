using Content.Shared.Hands;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle;

public abstract partial class SharedVehicleSystem
{
    private void InitializeRider()
    {
        SubscribeLocalEvent<RiderComponent, ComponentGetState>(OnRiderGetState);
        SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnPullAttempt);
    }

    private void OnRiderGetState(EntityUid uid, RiderComponent component, ref ComponentGetState args)
    {
        args.State = new RiderComponentState()
        {
            Entity = GetNetEntity(component.Vehicle),
        };
    }

    /// <summary>
    /// Kick the rider off the vehicle if they press q / drop the virtual item
    /// </summary>
    private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity == component.Vehicle)
        {
            _buckle.TryUnbuckle(uid, uid, true);
        }
    }

    private void OnPullAttempt(EntityUid uid, RiderComponent component, PullAttemptEvent args)
    {
        if (component.Vehicle != null)
            args.Cancelled = true;
    }
}
