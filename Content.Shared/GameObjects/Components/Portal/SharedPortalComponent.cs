using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Portal
{
    public abstract class SharedPortalComponent : Component
    {
        public override string Name => "Portal";

        public override void OnAdd()
        {
            base.OnAdd();

            if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
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
