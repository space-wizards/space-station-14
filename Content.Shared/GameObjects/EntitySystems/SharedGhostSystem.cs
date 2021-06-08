using System;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedGhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedGhostComponent, GhostChangeCanReturnToBodyEvent>(OnGhostChangeCanReturnToBody);
            SubscribeLocalEvent<SharedGhostComponent, GhostAddWarpNameEvent>(OnGhostAddWarpName);
        }

        private void OnGhostChangeCanReturnToBody(EntityUid uid, SharedGhostComponent component, GhostChangeCanReturnToBodyEvent args)
        {
            if (component.CanReturnToBody == args.CanReturnToBody)
            {
                return;
            }

            component.CanReturnToBody = args.CanReturnToBody;
            component.Dirty();
        }

        private void OnGhostAddWarpName(EntityUid uid, SharedGhostComponent component, GhostAddWarpNameEvent args)
        {
            if (component.LocationWarps.Add(component.Name))
            {
                component.Dirty();
            }
        }
    }

    [Serializable, NetSerializable]
    public class GhostChangeCanReturnToBodyEvent : EntityEventArgs
    {
        public GhostChangeCanReturnToBodyEvent(bool canReturnToBody)
        {
            CanReturnToBody = canReturnToBody;
        }

        public bool CanReturnToBody { get; }
    }

    [Serializable, NetSerializable]
    public class GhostAddWarpNameEvent : EntityEventArgs
    {
        public GhostAddWarpNameEvent(string warpName)
        {
            WarpName = warpName;
        }

        public string WarpName { get; }
    }

    [Serializable, NetSerializable]
    public class GhostWarpsRequestEvent : EntityEventArgs
    {
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
