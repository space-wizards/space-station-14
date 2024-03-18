using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Pulling
{
    /// <summary>
    /// This is the core of pulling state management.
    /// Because pulling state is such a mess to get right, all writes to pulling state must go through this class.
    /// </summary>
    [UsedImplicitly]
    public sealed class SharedPullingStateManagementSystem : EntitySystem
    {
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedPullableComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnShutdown(Entity<SharedPullableComponent> entity, ref ComponentShutdown args)
        {
            if (entity.Comp.Puller != null)
                ForceRelationship(null, entity);
        }

        // A WARNING:
        // The following 2 functions are the most internal part of the pulling system's relationship management.
        // They do not expect to be cancellable.
        private void ForceDisconnect(Entity<SharedPullerComponent?> puller, Entity<SharedPullableComponent?> pullable)
        {
            if (!Resolve(puller, ref puller.Comp))
                return;

            if (!Resolve(pullable, ref pullable.Comp))
                return;

            // MovingTo shutdown
            ForceSetMovingTo(pullable, null);

            // Joint shutdown
            if (!_timing.ApplyingState && // During state-handling, joint component will handle its own state.
                pullable.Comp.PullJointId != null &&
                TryComp(puller, out JointComponent? jointComp))
            {
                if (jointComp.GetJoints.TryGetValue(pullable.Comp.PullJointId, out var j))
                    _jointSystem.RemoveJoint(j);
            }
            pullable.Comp.PullJointId = null;

            // State shutdown
            puller.Comp.Pulling = null;
            pullable.Comp.Puller = null;

            // Messaging
            var message = new PullStoppedMessage(puller, pullable);

            RaiseLocalEvent(puller, message, broadcast: false);

            if (Initialized(pullable))
                RaiseLocalEvent(pullable, message, true);

            // Networking
            Dirty(puller);
            Dirty(pullable);
        }

        private void ForceDisconnect(EntityUid puller, EntityUid pullable, SharedPullerComponent? pullerComp = null, SharedPullableComponent? pullableComp = null)
        {
            ForceDisconnect((puller, pullerComp), (pullable, pullableComp));
        }

        public void ForceRelationship(EntityUid? pullerEnt, EntityUid? pullableEnt, SharedPullerComponent? puller = null, SharedPullableComponent? pullable = null)
        {
            if (_timing.ApplyingState)
                return;

            if (pullerEnt != null && !Resolve(pullerEnt.Value, ref puller))
                return;

            if (pullableEnt != null && !Resolve(pullableEnt.Value, ref pullable))
                return;

            if (pullable != null && puller != null && puller.Pulling == pullableEnt)
            {
                // Already done
                return;
            }

            // Start by disconnecting the pullable from whatever it is currently connected to.
            var pullableOldPullerE = pullable?.Puller;
            if (pullableOldPullerE != null && pullableEnt != null)
                ForceDisconnect(pullableOldPullerE.Value, pullableEnt.Value);

            // Continue with the puller.
            var pullerOldPullableE = puller?.Pulling;
            if (pullerOldPullableE != null && pullerEnt != null)
                ForceDisconnect(pullerEnt.Value!, pullerOldPullableE.Value);

            if (pullerEnt == null || !Resolve(pullerEnt.Value, ref puller))
                return;

            if (pullableEnt == null || !Resolve(pullableEnt.Value, ref pullable))
                return;

            // And now for the actual connection (if any).
            var pullablePhysics = Comp<PhysicsComponent>(pullableEnt.Value);
            pullable.PullJointId = $"pull-joint-{pullableEnt.Value}";

            // State startup
            puller.Pulling = pullableEnt;
            pullable.Puller = pullerEnt;

            // joint state handling will manage its own state
            if (!_timing.ApplyingState)
            {
                // Joint startup
                var union = _physics.GetHardAABB(pullerEnt.Value).Union(_physics.GetHardAABB(pullableEnt.Value, body: pullablePhysics));
                var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                var joint = _jointSystem.CreateDistanceJoint(pullableEnt.Value, pullerEnt.Value, id: pullable.PullJointId);
                joint.CollideConnected = false;
                // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
                joint.MaxLength = Math.Max(1.0f, length);
                joint.Length = length * 0.75f;
                joint.MinLength = 0f;
                joint.Stiffness = 1f;
            }

            // Messaging
            var message = new PullStartedMessage(pullerEnt.Value, pullableEnt.Value);

            RaiseLocalEvent(pullerEnt.Value, message, broadcast: false);
            RaiseLocalEvent(pullableEnt.Value, message, true);

            // Networking
            Dirty(pullerEnt.Value, puller);
            Dirty(pullableEnt.Value, pullable);
        }

        public void ForceRelationship(Entity<SharedPullerComponent?> puller, Entity<SharedPullableComponent?> pullable)
        {
            ForceRelationship(puller, pullable, puller.Comp, pullable.Comp);
        }

        // For OnRemove use only.
        public void ForceDisconnectPuller(Entity<SharedPullerComponent?> puller)
        {
            // DO NOT ADD ADDITIONAL LOGIC IN THIS FUNCTION. Do it in ForceRelationship.
            ForceRelationship(puller, null);
        }

        public void ForceDisconnectPuller(EntityUid puller, SharedPullerComponent? comp = null)
        {
            ForceDisconnectPuller((puller, comp));
        }

        // For OnRemove use only.
        public void ForceDisconnectPullable(Entity<SharedPullableComponent?> pullable)
        {
            // DO NOT ADD ADDITIONAL LOGIC IN THIS FUNCTION. Do it in ForceRelationship.
            ForceRelationship(null, pullable);
        }

        public void ForceDisconnectPullable(EntityUid pullable, SharedPullableComponent? comp = null)
        {
            ForceDisconnectPullable((pullable, comp));
        }

        public void ForceSetMovingTo(Entity<SharedPullableComponent?> pullable, EntityCoordinates? movingTo)
        {
            if (!Resolve(pullable, ref pullable.Comp))
                return;

            if (_timing.ApplyingState)
                return;

            if (pullable.Comp.MovingTo == movingTo)
                return;

            // Don't allow setting a MovingTo if there's no puller.
            // The other half of this guarantee (shutting down a MovingTo if the puller goes away) is enforced in ForceRelationship.
            if (pullable.Comp.Puller == null && movingTo != null)
                return;

            pullable.Comp.MovingTo = movingTo;
            Dirty(pullable);

            if (movingTo == null)
                RaiseLocalEvent(pullable.Owner, new PullableStopMovingMessage(), true);
            else
                RaiseLocalEvent(pullable.Owner, new PullableMoveMessage(), true);
        }

        public void ForceSetMovingTo(EntityUid pullable, EntityCoordinates? movingTo, SharedPullableComponent? comp = null)
        {
            ForceSetMovingTo((pullable, comp), movingTo);
        }

        /// <summary>
        /// Changes if the entity needs a hand in order to be able to pull objects.
        /// </summary>
        public void ChangeHandRequirement(Entity<SharedPullerComponent?> entity, bool needsHands)
        {
            if (!Resolve(entity, ref entity.Comp, false))
                return;

            entity.Comp.NeedsHands = needsHands;

            Dirty(entity);
        }

        public void ChangeHandRequirement(EntityUid uid, bool needsHands, SharedPullerComponent? comp = null)
        {
            ChangeHandRequirement((uid, comp), needsHands);
        }
    }
}
