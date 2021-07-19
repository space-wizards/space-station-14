using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.Portal.Components
{
    public abstract class SharedPortalComponent : Component
    {
        public override string Name => "Portal";

        protected override void OnAdd()
        {
            base.OnAdd();

            if (Owner.TryGetComponent<IPhysBody>(out var physics))
            {
                physics.Hard = false;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum PortalVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum PortalState
    {
        RecentlyTeleported,
        Pending,
        UnableToTeleport,
    }

}
