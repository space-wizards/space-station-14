using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Pulling
{
    public abstract partial class SharedPullingSystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public bool CanPull(EntityUid puller, EntityUid pulled)
        {
            if (!TryComp<SharedPullerComponent>(puller, out var comp))
                return false;

            if (comp.NeedsHands && !_handsSystem.TryGetEmptyHand(puller, out _))
                return false;

            if (!_blocker.CanInteract(puller, pulled))
                return false;

            if (!TryComp<PhysicsComponent>(pulled, out var physics))
                return false;

            if (physics.BodyType == BodyType.Static)
                return false;

            if (puller == pulled)
                return false;

            if (_containerSystem.IsEntityInContainer(puller) || _containerSystem.IsEntityInContainer(pulled))
                return false;

            if (TryComp<BuckleComponent>(puller, out var buckle))
            {
                // Prevent people pulling the chair they're on, etc.
                if (buckle is { PullStrap: false, Buckled: true } && buckle.LastEntityBuckledTo == pulled)
                    return false;
            }

            var getPulled = new BeingPulledAttemptEvent(puller, pulled);
            RaiseLocalEvent(pulled, getPulled, true);
            var startPull = new StartPullAttemptEvent(puller, pulled);
            RaiseLocalEvent(puller, startPull, true);
            return (!startPull.Cancelled && !getPulled.Cancelled);
        }

        public bool TogglePull(EntityUid puller, EntityUid pullable, SharedPullerComponent? pullerComp = null, SharedPullableComponent? pullableComp = null)
        {
            if (!Resolve(puller, ref pullerComp))
                return false;

            if (!Resolve(pullable, ref pullableComp))
                return false;

            return TogglePull((puller, pullerComp), (pullable, pullableComp));
        }

        public bool TogglePull(Entity<SharedPullerComponent?> puller, Entity<SharedPullableComponent?> pullable)
        {
            if (pullable.Comp?.Puller == puller)
                return TryStopPull(pullable);

            return TryStartPull(puller, pullable);
        }

        // -- Core attempted actions --

        public bool TryStopPull(EntityUid pullable, SharedPullableComponent? comp = null, EntityUid? user = null)
        {
            return TryStopPull((pullable, comp), user);
        }

        public bool TryStopPull(Entity<SharedPullableComponent?> pullable, EntityUid? user = null)
        {
            if (!Resolve(pullable, ref pullable.Comp))
                return false;

            if (_timing.ApplyingState)
                return false;

            if (!pullable.Comp.BeingPulled)
            {
                return false;
            }

            var msg = new StopPullingEvent(user);
            RaiseLocalEvent(pullable, msg, true);

            if (msg.Cancelled) return false;

            // Stop pulling confirmed!

            if (TryComp<PhysicsComponent>(pullable, out var pullablePhysics))
            {
                _physics.SetFixedRotation(pullable, pullable.Comp.PrevFixedRotation, body: pullablePhysics);
            }

            _pullSm.ForceRelationship(null, pullable);
            return true;
        }

        public bool TryStartPull(EntityUid puller, EntityUid pullable, SharedPullerComponent? pullerComp = null, SharedPullableComponent? pullableComp = null)
        {
            return TryStartPull((puller, pullerComp), (pullable, pullableComp));
        }

        // The main "start pulling" function.
        public bool TryStartPull(Entity<SharedPullerComponent?> puller, Entity<SharedPullableComponent?> pullable)
        {
            if (!Resolve(puller, ref puller.Comp) || !Resolve(pullable, ref pullable.Comp))
                return false;

            if (_timing.ApplyingState)
                return false;

            if (puller.Comp.Pulling == pullable)
                return true;

            // Pulling a new object : Perform sanity checks.

            if (!CanPull(puller, pullable))
                return false;

            if (!HasComp<PhysicsComponent>(puller))
                return false;

            if (!TryComp<PhysicsComponent>(pullable, out var pullablePhysics))
                return false;

            // Ensure that the puller is not currently pulling anything.
            // If this isn't done, then it happens too late, and the start/stop messages go out of order,
            //  and next thing you know it thinks it's not pulling anything even though it is!

            var oldPullable = puller.Comp.Pulling;
            if (oldPullable != null)
            {
                if (TryComp<SharedPullableComponent>(oldPullable.Value, out var oldPullableComp))
                {
                    if (!TryStopPull(oldPullable.Value, oldPullableComp))
                        return false;
                }
                else
                {
                    Log.Warning("Well now you've done it, haven't you? Someone transferred pulling (onto {0}) while presently pulling something that has no Pullable component (on {1})!", pullable.Owner, oldPullable);
                    return false;
                }
            }

            // Ensure that the pullable is not currently being pulled.
            // Same sort of reasons as before.

            var oldPuller = pullable.Comp.Puller;
            if (oldPuller != null)
            {
                if (!TryStopPull(pullable))
                    return false;
            }

            // Continue with pulling process.

            var pullAttempt = new PullAttemptEvent(puller.Owner, pullable);

            RaiseLocalEvent(puller, pullAttempt, broadcast: false);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            RaiseLocalEvent(pullable, pullAttempt, true);

            if (pullAttempt.Cancelled)
                return false;

            _interaction.DoContactInteraction(pullable, puller);

            _pullSm.ForceRelationship(puller, pullable);
            pullable.Comp.PrevFixedRotation = pullablePhysics.FixedRotation;
            _physics.SetFixedRotation(pullable, pullable.Comp.FixedRotationOnPull, body: pullablePhysics);
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(puller):user} started pulling {ToPrettyString(pullable):target}");
            return true;
        }

        public bool TryMoveTo(EntityUid pullable, EntityCoordinates to, SharedPullableComponent? component = null)
        {
            return TryMoveTo((pullable, component), to);
        }

        public bool TryMoveTo(Entity<SharedPullableComponent?> pullable, EntityCoordinates to)
        {
            if (!Resolve(pullable, ref pullable.Comp))
                return false;

            if (pullable.Comp.Puller == null)
                return false;

            if (!HasComp<PhysicsComponent>(pullable))
                return false;

            _pullSm.ForceSetMovingTo(pullable, to);
            return true;
        }

        public void StopMoveTo(EntityUid pullable, SharedPullableComponent? comp = null)
        {
            StopMoveTo((pullable, comp));
        }

        public void StopMoveTo(Entity<SharedPullableComponent?> pullable)
        {
            if (!Resolve(pullable, ref pullable.Comp))
                return;

            _pullSm.ForceSetMovingTo(pullable, null);
        }
    }
}
