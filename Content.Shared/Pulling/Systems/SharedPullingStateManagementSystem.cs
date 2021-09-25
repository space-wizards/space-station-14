using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Shared.Pulling
{
    /// <summary>
    /// This is the core of pulling state management.
    /// Because pulling state is such a mess to get right, all writes to pulling state must go through this class.
    /// </summary>
    [UsedImplicitly]
    public class SharedPullingStateManagementSystem : EntitySystem
    {
        // A WARNING:
        // The following function is the most internal part of the pulling system's relationship management.
        // It does not expect to be cancellable.

        public void ForceRelationship(SharedPullerComponent? puller, SharedPullableComponent? pullable)
        {
            if ((puller != null) && (puller.Pulling == pullable))
            {
                // Already done
                return;
            }

            var eventBus = (pullable?.Owner ?? puller?.Owner)!.EntityManager.EventBus;

            var pullerPhysics = puller?.Owner.GetComponentOrNull<PhysicsComponent>();
            var pullablePhysics = pullable?.Owner.GetComponentOrNull<PhysicsComponent>();

            // Start by disconnecting the puller from whatever it is currently connected to.
            var pullerOldPullableE = puller?.Pulling;
            if (pullerOldPullableE != null)
            {
                // It's important to note: This *can* return null if we're doing this because the component is being removed.
                // Therefore, parts that rely on this component must be skippable.
                pullerOldPullableE.TryGetComponent<SharedPullableComponent>(out var pullerOldPullable);

                // Joint shutdown
                if (pullerOldPullable?.PullJoint != null)
                {
                    pullerPhysics?.RemoveJoint(pullerOldPullable.PullJoint);
                    pullerOldPullable.PullJoint = null;
                }

                // State shutdown
                puller!.Pulling = null;
                pullerOldPullable?.UpdatePullerFromSharedPullingStateManagementSystem(null);

                // Messaging
                var message = new PullStoppedMessage(pullerPhysics!, pullerOldPullableE.GetComponent<IPhysBody>());

                eventBus.RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);
                // TODO: FIGURE OUT WHY THIS CRASHES IF THE COMPONENT IS BEING REMOVED.
                // TODO: Work out why. Monkey + meat spike is a good test for this,
                //  assuming you're still pulling the monkey when it gets gibbed.
                if (pullerOldPullable != null)
                {
                    eventBus.RaiseLocalEvent(pullerOldPullableE.Uid, message);
                }

                // Networking
                pullerOldPullable?.Dirty();
            }

            // Continue by doing that with the pullable.
            if (pullable?.Puller != null)
            {
                ForceRelationship(pullable.Puller.GetComponent<SharedPullerComponent>(), null);
            }

            // And now for the actual connection (if any).

            if ((puller != null) && (pullable != null))
            {
                // Physics final sanity check
                DebugTools.AssertNotNull(pullerPhysics);
                DebugTools.AssertNotNull(pullablePhysics);

                // State startup
                puller.Pulling = pullable.Owner;
                pullable.UpdatePullerFromSharedPullingStateManagementSystem(puller.Owner);

                // Messaging
                var message = new PullStartedMessage(pullerPhysics!, pullablePhysics!);

                eventBus.RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);
                eventBus.RaiseLocalEvent(pullable.Owner.Uid, message);

                // Joint startup
                var union = pullerPhysics!.GetWorldAABB().Union(pullablePhysics!.GetWorldAABB());
                var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                pullable.PullJoint = pullerPhysics.CreateDistanceJoint(pullablePhysics, $"pull-joint-{pullablePhysics.Owner.Uid}");
                pullable.PullJoint.CollideConnected = false;
                pullable.PullJoint.Length = length * 0.75f;
                pullable.PullJoint.MaxLength = length;
            }

            // Update puller state.
            // This might be better suited to an event handler somewhere, not sure.

            if (puller != null)
            {
                if (puller.Owner.TryGetComponent<MovementSpeedModifierComponent>(out var speed))
                {
                    speed.RefreshMovementSpeedModifiers();
                }
            }

            // Networking
            puller?.Dirty();
            pullable?.Dirty();
        }

        // For OnRemove use only.
        public void ForceDisconnectPuller(SharedPullerComponent puller)
        {
            ForceRelationship(puller, null);
        }

        // For OnRemove use only.
        public void ForceDisconnectPullable(SharedPullableComponent pullable)
        {
            ForceRelationship(null, pullable);
        }
    }
}
