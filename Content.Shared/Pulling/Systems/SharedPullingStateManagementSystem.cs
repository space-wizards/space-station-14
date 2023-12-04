using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
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
            SubscribeLocalEvent<SharedPullableComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedPullableComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, SharedPullableComponent component, ref ComponentGetState args)
        {
            args.State = new PullableComponentState(GetNetEntity(component.Puller));
        }

        private void OnHandleState(EntityUid uid, SharedPullableComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PullableComponentState state)
                return;

            var puller = EnsureEntity<SharedPullableComponent>(state.Puller, uid);

            if (!puller.HasValue)
            {
                ForceDisconnectPullable(component);
                return;
            }

            if (component.Puller == puller)
            {
                // don't disconnect and reconnect a puller for no reason
                return;
            }

            if (!TryComp<SharedPullerComponent>(puller, out var comp))
            {
                Log.Error($"Pullable state for entity {ToPrettyString(uid)} had invalid puller entity {ToPrettyString(puller.Value)}");
                // ensure it disconnects from any different puller, still
                ForceDisconnectPullable(component);
                return;
            }

            ForceRelationship(comp, component);
        }

        private void OnShutdown(EntityUid uid, SharedPullableComponent component, ComponentShutdown args)
        {
            if (component.Puller != null)
                ForceRelationship(null, component);
        }

        // A WARNING:
        // The following 2 functions are the most internal part of the pulling system's relationship management.
        // They do not expect to be cancellable.
        private void ForceDisconnect(SharedPullerComponent puller, SharedPullableComponent pullable)
        {
            var pullerPhysics = EntityManager.GetComponent<PhysicsComponent>(puller.Owner);
            var pullablePhysics = EntityManager.GetComponent<PhysicsComponent>(pullable.Owner);

            // MovingTo shutdown
            ForceSetMovingTo(pullable, null);

            // Joint shutdown
            if (!_timing.ApplyingState && // During state-handling, joint component will handle its own state.
                pullable.PullJointId != null &&
                TryComp(puller.Owner, out JointComponent? jointComp))
            {
                if (jointComp.GetJoints.TryGetValue(pullable.PullJointId, out var j))
                    _jointSystem.RemoveJoint(j);
            }
            pullable.PullJointId = null;

            // State shutdown
            puller.Pulling = null;
            pullable.Puller = null;

            // Messaging
            var message = new PullStoppedMessage(pullerPhysics, pullablePhysics);

            RaiseLocalEvent(puller.Owner, message, broadcast: false);

            if (Initialized(pullable.Owner))
                RaiseLocalEvent(pullable.Owner, message, true);

            // Networking
            Dirty(puller);
            Dirty(pullable);
        }

        public void ForceRelationship(SharedPullerComponent? puller, SharedPullableComponent? pullable)
        {
            if (pullable != null && puller != null && (puller.Pulling == pullable.Owner))
            {
                // Already done
                return;
            }

            // Start by disconnecting the pullable from whatever it is currently connected to.
            var pullableOldPullerE = pullable?.Puller;
            if (pullableOldPullerE != null)
            {
                ForceDisconnect(EntityManager.GetComponent<SharedPullerComponent>(pullableOldPullerE.Value), pullable!);
            }

            // Continue with the puller.
            var pullerOldPullableE = puller?.Pulling;
            if (pullerOldPullableE != null)
            {
                ForceDisconnect(puller!, EntityManager.GetComponent<SharedPullableComponent>(pullerOldPullableE.Value));
            }

            // And now for the actual connection (if any).

            if (puller != null && pullable != null)
            {
                var pullerPhysics = EntityManager.GetComponent<PhysicsComponent>(puller.Owner);
                var pullablePhysics = EntityManager.GetComponent<PhysicsComponent>(pullable.Owner);
                pullable.PullJointId = $"pull-joint-{pullable.Owner}";

                // State startup
                puller.Pulling = pullable.Owner;
                pullable.Puller = puller.Owner;

                // joint state handling will manage its own state
                if (!_timing.ApplyingState)
                {
                    // Joint startup
                    var union = _physics.GetHardAABB(puller.Owner).Union(_physics.GetHardAABB(pullable.Owner, body: pullablePhysics));
                    var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                    var joint = _jointSystem.CreateDistanceJoint(pullablePhysics.Owner, pullerPhysics.Owner, id: pullable.PullJointId);
                    joint.CollideConnected = false;
                    // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
                    joint.MaxLength = Math.Max(1.0f, length);
                    joint.Length = length * 0.75f;
                    joint.MinLength = 0f;
                    joint.Stiffness = 1f;
                }

                // Messaging
                var message = new PullStartedMessage(pullerPhysics, pullablePhysics);

                RaiseLocalEvent(puller.Owner, message, broadcast: false);
                RaiseLocalEvent(pullable.Owner, message, true);

                // Networking
                Dirty(puller);
                Dirty(pullable);
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
            if (pullable.Puller == null && movingTo != null)
            {
                return;
            }

            pullable.MovingTo = movingTo;

            if (movingTo == null)
            {
                RaiseLocalEvent(pullable.Owner, new PullableStopMovingMessage(), true);
            }
            else
            {
                RaiseLocalEvent(pullable.Owner, new PullableMoveMessage(), true);
            }
        }

        /// <summary>
        /// Changes if the entity needs a hand in order to be able to pull objects.
        /// </summary>
        public void ChangeHandRequirement(EntityUid uid, bool needsHands, SharedPullerComponent? comp)
        {
            if (!Resolve(uid, ref comp, false))
                return;

            comp.NeedsHands = needsHands;

            Dirty(uid, comp);
        }
    }
}
