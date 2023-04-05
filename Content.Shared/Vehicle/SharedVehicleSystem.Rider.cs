using Content.Shared.Physics.Pull;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

public abstract partial class SharedVehicleSystem
{
    [Serializable, NetSerializable]
    protected sealed class RiderComponentState : ComponentState
    {
        public EntityUid? Entity;
    }

    private void OnRiderPull(EntityUid uid, RiderComponent component, PullAttemptEvent args)
    {
        if (component.Vehicle != null)
            args.Cancelled = true;
    }
}
