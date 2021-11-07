using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Content.Shared.Rotatable;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Log;

namespace Content.Shared.Pulling
{
    public abstract partial class SharedPullingSystem : EntitySystem
    {
        public bool CanPull(IEntity puller, IEntity pulled)
        {
            if (!puller.HasComponent<SharedPullerComponent>())
            {
                return false;
            }

            if (!pulled.TryGetComponent<IPhysBody>(out var _physics))
            {
                return false;
            }

            if (_physics.BodyType == BodyType.Static)
            {
                return false;
            }

            if (puller == pulled)
            {
                return false;
            }

            if (!puller.IsInSameOrNoContainer(pulled))
            {
                return false;
            }

            var startPull = new StartPullAttemptEvent(puller, pulled);
            RaiseLocalEvent(puller.Uid, startPull);
            return !startPull.Cancelled;
        }

        public bool TogglePull(IEntity puller, SharedPullableComponent pullable)
        {
            if (pullable.Puller == puller)
            {
                return TryStopPull(pullable);
            }
            return TryStartPull(puller, pullable.Owner);
        }

        // -- Core attempted actions --

        public bool TryStopPull(SharedPullableComponent pullable, IEntity? user = null)
        {
            if (!pullable.BeingPulled)
            {
                return false;
            }

            var msg = new StopPullingEvent(user?.Uid);
            RaiseLocalEvent(pullable.Owner.Uid, msg);

            if (msg.Cancelled) return false;

            _pullSm.ForceRelationship(null, pullable);
            return true;
        }

        public bool TryStartPull(IEntity puller, IEntity pullable)
        {
            if (!puller.TryGetComponent<SharedPullerComponent>(out var pullerComp))
            {
                return false;
            }
            if (!pullable.TryGetComponent<SharedPullableComponent>(out var pullableComp))
            {
                return false;
            }
            return TryStartPull(pullerComp, pullableComp);
        }

        // The main "start pulling" function.
        public bool TryStartPull(SharedPullerComponent puller, SharedPullableComponent pullable)
        {
            if (puller.Pulling == pullable)
                return true;

            // Pulling a new object : Perform sanity checks.

            if (!EntitySystem.Get<SharedPullingSystem>().CanPull(puller.Owner, pullable.Owner))
            {
                return false;
            }

            if (!puller.Owner.TryGetComponent<PhysicsComponent>(out var pullerPhysics))
            {
                return false;
            }

            if (!pullable.Owner.TryGetComponent<PhysicsComponent>(out var pullablePhysics))
            {
                return false;
            }

            // Ensure that the puller is not currently pulling anything.
            // If this isn't done, then it happens too late, and the start/stop messages go out of order,
            //  and next thing you know it thinks it's not pulling anything even though it is!

            var oldPullable = puller.Pulling;
            if (oldPullable != null)
            {
                if (oldPullable.TryGetComponent<SharedPullableComponent>(out var oldPullableComp))
                {
                    if (!TryStopPull(oldPullableComp))
                    {
                        return false;
                    }
                }
                else
                {
                    Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? Someone transferred pulling (onto {0}) while presently pulling something that has no Pullable component (on {1})!", pullable.Owner, oldPullable);
                    return false;
                }
            }

            // Ensure that the pullable is not currently being pulled.
            // Same sort of reasons as before.

            var oldPuller = pullable.Puller;
            if (oldPuller != null)
            {
                if (!TryStopPull(pullable))
                {
                    return false;
                }
            }

            // Continue with pulling process.

            var pullAttempt = new PullAttemptMessage(pullerPhysics, pullablePhysics);

            RaiseLocalEvent(puller.Owner.Uid, pullAttempt, broadcast: false);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            RaiseLocalEvent(pullable.Owner.Uid, pullAttempt);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            _pullSm.ForceRelationship(puller, pullable);
            return true;
        }

        public bool TryMoveTo(SharedPullableComponent pullable, EntityCoordinates to)
        {
            if (pullable.Puller == null)
            {
                return false;
            }

            if (!pullable.Owner.HasComponent<PhysicsComponent>())
            {
                return false;
            }

            _pullSm.ForceSetMovingTo(pullable, to);
            return true;
        }

        public void StopMoveTo(SharedPullableComponent pullable)
        {
            _pullSm.ForceSetMovingTo(pullable, null);
        }
    }
}
