using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    public abstract class SharedGhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public void SetCanReturnToBody(SharedGhostComponent component, bool canReturn)
        {
            if (component.CanReturnToBody == canReturn)
            {
                return;
            }

            component.CanReturnToBody = canReturn;
            component.Dirty();
        }
    }

    [Serializable, NetSerializable]
    public class GhostWarpsRequestEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class GhostWarpsResponseEvent : EntityEventArgs
    {
        public GhostWarpsResponseEvent(List<string> locations, Dictionary<EntityUid, string> players)
        {
            Locations = locations;
            Players = players;
        }

        public List<string> Locations { get; }

        public Dictionary<EntityUid, string> Players { get; }
    }

    [Serializable, NetSerializable]
    public class GhostWarpToLocationRequestEvent : EntityEventArgs
    {
        public string Name { get; }

        public GhostWarpToLocationRequestEvent(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public class GhostWarpToTargetRequestEvent : EntityEventArgs
    {
        public EntityUid Target { get; }

        public GhostWarpToTargetRequestEvent(EntityUid target)
        {
            Target = target;
        }
    }

    [Serializable, NetSerializable]
    public class GhostReturnToBodyRequest : EntityEventArgs
    {
    }
}
