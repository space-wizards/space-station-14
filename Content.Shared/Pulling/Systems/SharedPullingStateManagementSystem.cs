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
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Log;

namespace Content.Shared.Pulling
{
    [UsedImplicitly]
    public class SharedPullingStateManagementSystem : EntitySystem
    {
        // A WARNING:
        // The following function is the most internal part of the pulling system's relationship management.
        // It does not expect to be cancellable.

        public static void ForceRelationship(SharedPullerComponent? puller, SharedPullableComponent? pullable)
        {
            if ((puller != null) && (puller.Pulling == pullable))
            {
                // Already done
                return;
            }

            var eventBus = (pullable?.Owner ?? puller?.Owner)!.EntityManager.EventBus;

            var pullerPhysics = puller?.Owner.GetComponent<PhysicsComponent>();
            var pullablePhysics = pullable?.Owner.GetComponent<PhysicsComponent>();

            // Start by disconnecting the puller from whatever it is currently connected to.
            var pullerOldPullableE = puller?.Pulling;
            if (pullerOldPullableE != null)
            {
                // Joint shutdown
                var pullerOldPullable = pullerOldPullableE.GetComponent<SharedPullableComponent>();
                if (pullerOldPullable._pullJoint != null)
                {
                    pullerPhysics!.RemoveJoint(pullerOldPullable._pullJoint);
                }
                pullerOldPullable._pullJoint = null;

                // State shutdown
                puller!.Pulling = null;
                pullerOldPullable._puller = null;

                // Messaging
                var message = new PullStoppedMessage(pullerPhysics!, pullerOldPullableE.GetComponent<IPhysBody>());

                eventBus.RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);
                eventBus.RaiseLocalEvent(pullerOldPullableE.Uid, message);
            }

            // Continue by doing that with the pullable.
            if (pullable?.Puller != null)
            {
                ForceRelationship(pullable.Puller.GetComponent<SharedPullerComponent>(), null);
            }

            // And now for the actual connection (if any).

            if ((puller != null) && (pullable != null))
            {
                // State startup
                puller.Pulling = pullable.Owner;
                pullable._puller = puller.Owner;

                // Messaging
                var message = new PullStartedMessage(pullerPhysics!, pullablePhysics!);

                eventBus.RaiseLocalEvent(puller.Owner.Uid, message, broadcast: false);
                eventBus.RaiseLocalEvent(pullable.Owner.Uid, message);

                // Joint startup
                var union = pullerPhysics!.GetWorldAABB().Union(pullablePhysics!.GetWorldAABB());
                var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                pullable._pullJoint = pullerPhysics.CreateDistanceJoint(pullablePhysics, $"pull-joint-{pullablePhysics.Owner.Uid}");
                pullable._pullJoint.CollideConnected = false;
                pullable._pullJoint.Length = length * 0.75f;
                pullable._pullJoint.MaxLength = length;
            }

            if (puller != null)
            {
                if (puller.Owner.TryGetComponent<MovementSpeedModifierComponent>(out var speed))
                {
                    speed.RefreshMovementSpeedModifiers();
                }
            }

            puller?.Dirty();
            pullable?.Dirty();
        }

        // The main "start pulling" function.
        public static void StartPulling(SharedPullerComponent puller, SharedPullableComponent pullable)
        {
            if (puller.Pulling == pullable)
                return;

            var eventBus = pullable.Owner.EntityManager.EventBus;

            // Pulling a new object : Perform sanity checks.

            if (!EntitySystem.Get<SharedPullingSystem>().CanPull(puller.Owner, pullable.Owner))
            {
                return;
            }

            if (!puller.Owner.TryGetComponent<PhysicsComponent>(out var pullerPhysics))
            {
                return;
            }

            if (!pullable.Owner.TryGetComponent<PhysicsComponent>(out var pullablePhysics))
            {
                return;
            }

            // Ensure that the puller is not currently pulling anything.
            // If this isn't done, then it happens too late, and the start/stop messages go out of order,
            //  and next thing you know it thinks it's not pulling anything even though it is!

            var oldPullable = puller.Pulling;
            if (oldPullable != null)
            {
                if (oldPullable.TryGetComponent<SharedPullableComponent>(out var oldPullableComp))
                {
                    if (!oldPullableComp.TryStopPull())
                    {
                        return;
                    }
                }
                else
                {
                    Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? Someone transferred pulling (onto {0}) while presently pulling something that has no Pullable component (on {1})!", pullable.Owner, oldPullable);
                    return;
                }
            }

            // Ensure that the pullable is not currently being pulled.
            // Same sort of reasons as before.

            var oldPuller = pullable.Puller;
            if (oldPuller != null)
            {
                if (!pullable.TryStopPull())
                {
                    return;
                }
            }

            // Continue with pulling process.

            var pullAttempt = new PullAttemptMessage(pullerPhysics, pullablePhysics);

            eventBus.RaiseLocalEvent(puller.Owner.Uid, pullAttempt, broadcast: false);

            if (pullAttempt.Cancelled)
            {
                return;
            }

            eventBus.RaiseLocalEvent(pullable.Owner.Uid, pullAttempt);

            if (pullAttempt.Cancelled)
            {
                return;
            }

            ForceRelationship(puller, pullable);
        }
    }
}
