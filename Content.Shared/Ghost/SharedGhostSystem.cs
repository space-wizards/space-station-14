using System;
using System.Collections.Generic;
using Content.Shared.DragDrop;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
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
            SubscribeLocalEvent<SharedGhostComponent, AttackAttemptEvent>(OnAttempt);
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
    /// A server to client response for a <see cref="GhostWarpsRequestEvent"/>.
    /// Contains players, and locations a ghost can warp to
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpsResponseEvent : EntityEventArgs
    {
        public GhostWarpsResponseEvent(List<string> locations, Dictionary<EntityUid, string> players)
        {
            Locations = locations;
            Players = players;
        }

        /// <summary>
        /// A list of location names that can be warped to.
        /// </summary>
        public List<string> Locations { get; }

        /// <summary>
        /// A dictionary containing the entity id, and name of players that can be warped to.
        /// </summary>
        public Dictionary<EntityUid, string> Players { get; }
    }

    /// <summary>
    /// A client to server request for their ghost to be warped to a location
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpToLocationRequestEvent : EntityEventArgs
    {
        /// <summary>
        /// The location name to warp to.
        /// </summary>
        public string Name { get; }

        public GhostWarpToLocationRequestEvent(string locationName)
        {
            Name = locationName;
        }
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
