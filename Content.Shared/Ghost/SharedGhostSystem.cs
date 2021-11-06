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
    public abstract class SharedGhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedGhostComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<SharedGhostComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<SharedGhostComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<SharedGhostComponent, AttackAttemptEvent>(OnAttackAttempt);
            SubscribeLocalEvent<SharedGhostComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<SharedGhostComponent, PickupAttemptEvent>(OnPickupAttempt);
        }

        private void OnUseAttempt(EntityUid uid, SharedGhostComponent component, UseAttemptEvent args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, SharedGhostComponent component, InteractionAttemptEvent args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
        }

        private void OnEmoteAttempt(EntityUid uid, SharedGhostComponent component, EmoteAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnAttackAttempt(EntityUid uid, SharedGhostComponent component, AttackAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, SharedGhostComponent component, DropAttemptEvent args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, SharedGhostComponent component, PickupAttemptEvent args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
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

    [Serializable, NetSerializable]
    public class GhostUpdateGhostRoleCountEvent : EntityEventArgs
    {
        public int AvailableGhostRoles { get; }

        public GhostUpdateGhostRoleCountEvent(int v)
        {
            AvailableGhostRoles = v;
        }
    }
}
