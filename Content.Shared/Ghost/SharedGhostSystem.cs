using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    /// <summary>
    /// System for the <see cref="SharedGhostComponent"/>.
    /// Prevents ghosts from interacting when <see cref="SharedGhostComponent.CanGhostInteract"/> is false.
    /// </summary>
    public abstract class SharedGhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedGhostComponent, UseAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<SharedGhostComponent, InteractionAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<SharedGhostComponent, EmoteAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<SharedGhostComponent, DropAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<SharedGhostComponent, PickupAttemptEvent>(OnAttempt);
        }

        private void OnAttempt(EntityUid uid, SharedGhostComponent component, CancellableEntityEventArgs args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
        }

        public void SetCanReturnToBody(SharedGhostComponent component, bool value)
        {
            component.CanReturnToBody = value;
        }
    }

    /// <summary>
    /// A client to server request to get places a ghost can warp to.
    /// Response is sent via <see cref="GhostWarpsResponseEvent"/>
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpsRequestEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// An individual place a ghost can warp to.
    /// This is used as part of <see cref="GhostWarpsResponseEvent"/>
    /// </summary>
    [Serializable, NetSerializable]
    public struct GhostWarp
    {
        public GhostWarp(EntityUid entity, string displayName, bool isWarpPoint)
        {
            Entity = entity;
            DisplayName = displayName;
            IsWarpPoint = isWarpPoint;
        }
        
        /// <summary>
        /// The entity representing the warp point.
        /// This is passed back to the server in <see cref="GhostWarpToTargetRequestEvent"/>
        /// </summary>
        public EntityUid Entity { get; }
        /// <summary>
        /// The display name to be surfaced in the ghost warps menu
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// Whether this warp represents a warp point or a player
        /// </summary>
        public bool IsWarpPoint { get;  }
    }

    /// <summary>
    /// A server to client response for a <see cref="GhostWarpsRequestEvent"/>.
    /// Contains players, and locations a ghost can warp to
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpsResponseEvent : EntityEventArgs
    {
        public GhostWarpsResponseEvent(List<GhostWarp> warps)
        {
            Warps = warps;
        }

        /// <summary>
        /// A list of warp points.
        /// </summary>
        public List<GhostWarp> Warps { get; }
    }

    /// <summary>
    ///  A client to server request for their ghost to be warped to an entity
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpToTargetRequestEvent : EntityEventArgs
    {
        public EntityUid Target { get; }

        public GhostWarpToTargetRequestEvent(EntityUid target)
        {
            Target = target;
        }
    }

    /// <summary>
    /// A client to server request for their ghost to return to body
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostReturnToBodyRequest : EntityEventArgs
    {
    }

    /// <summary>
    /// A server to client update with the available ghost role count
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostUpdateGhostRoleCountEvent : EntityEventArgs
    {
        public int AvailableGhostRoles { get; }

        public GhostUpdateGhostRoleCountEvent(int availableGhostRoleCount)
        {
            AvailableGhostRoles = availableGhostRoleCount;
        }
    }
}
