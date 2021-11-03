using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
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
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        // A WARNING:
        // The following 2 functions are the most internal part of the pulling system's relationship management.
        // They do not expect to be cancellable.
        private void ForceDisconnect(SharedPullerComponent puller, SharedPullableComponent pullable)
        {
            var pullerPhysics = puller.Owner.GetComponent<PhysicsComponent>();
            var pullablePhysics = pullable.Owner.GetComponent<PhysicsComponent>();

            // MovingTo shutdown
            ForceSetMovingTo(pullable, null);

            // Joint shutdown
            if (puller.Owner.TryGetComponent<JointComponent>(out var jointComp))
            {
                if (jointComp.GetJoints.Contains(pullable.PullJoint!))
                {
                    _jointSystem.RemoveJoint(pullable.PullJoint!);
                }
            }
            pullable.PullJoint = null;

            // State shutdown
            puller.Pulling = null;
            pullable.Puller = null;

            // Messaging
            var message = new PullStoppedMessage(pullerPhysics, pullablePhysics);

            RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);

            if (pullable.Owner.LifeStage <= EntityLifeStage.MapInitialized)
                RaiseLocalEvent(pullable.Owner.Uid, message);

            // Networking
            puller.Dirty();
            pullable.Dirty();
        }

        public void ForceRelationship(SharedPullerComponent? puller, SharedPullableComponent? pullable)
        {
            if ((puller != null) && (puller.Pulling == pullable))
            {
                // Already done
                return;
            }

            // Start by disconnecting the pullable from whatever it is currently connected to.
            var pullableOldPullerE = pullable?.Puller;
            if (pullableOldPullerE != null)
            {
                ForceDisconnect(pullableOldPullerE.GetComponent<SharedPullerComponent>(), pullable!);
            }

            // Continue with the puller.
            var pullerOldPullableE = puller?.Pulling;
            if (pullerOldPullableE != null)
            {
                ForceDisconnect(puller!, pullerOldPullableE.GetComponent<SharedPullableComponent>());
            }

            // And now for the actual connection (if any).

            if ((puller != null) && (pullable != null))
            {
                var pullerPhysics = puller.Owner.GetComponent<PhysicsComponent>();
                var pullablePhysics = pullable.Owner.GetComponent<PhysicsComponent>();

                // State startup
                puller.Pulling = pullable.Owner;
                pullable.Puller = puller.Owner;

                // Joint startup
                var union = pullerPhysics.GetWorldAABB().Union(pullablePhysics.GetWorldAABB());
                var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                pullable.PullJoint = _jointSystem.CreateDistanceJoint(pullablePhysics.Owner.Uid, pullerPhysics.Owner.Uid, id:$"pull-joint-{pullablePhysics.Owner.Uid}");
                pullable.PullJoint.CollideConnected = false;
                // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
                pullable.PullJoint.MaxLength = Math.Max(1.0f, length);
                pullable.PullJoint.Length = length * 0.75f;
                pullable.PullJoint.MinLength = 0f;
                pullable.PullJoint.Stiffness = 1f;

                // Messaging
                var message = new PullStartedMessage(pullerPhysics, pullablePhysics);

                RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);
                RaiseLocalEvent(pullable.Owner.Uid, message);

                // Networking
                puller.Dirty();
                pullable.Dirty();
            }
        }

        // For OnRemove use only.
        public void ForceDisconnectPuller(SharedPullerComponent puller)
        {
            // DO NOT ADD ADDITIONAL LOGIC IN THIS FUNCTION. Do it in ForceRelationship.
            ForceRelationship(puller, null);
        }

        // For OnRemove use only.
        public void ForceDisconnectPullable(SharedPullableComponent pullable)
        {
            // DO NOT ADD ADDITIONAL LOGIC IN THIS FUNCTION. Do it in ForceRelationship.
            ForceRelationship(null, pullable);
        }

        public void ForceSetMovingTo(SharedPullableComponent pullable, EntityCoordinates? movingTo)
        {
            if (pullable.MovingTo == movingTo)
            {
                return;
            }

            // Don't allow setting a MovingTo if there's no puller.
            // The other half of this guarantee (shutting down a MovingTo if the puller goes away) is enforced in ForceRelationship.
            if ((pullable.Puller == null) && (movingTo != null))
            {
                return;
            }

            pullable.MovingTo = movingTo;

            if (movingTo == null)
            {
                RaiseLocalEvent(pullable.Owner.Uid, new PullableStopMovingMessage());
            }
            else
            {
                RaiseLocalEvent(pullable.Owner.Uid, new PullableMoveMessage());
            }
        }
    }
}
