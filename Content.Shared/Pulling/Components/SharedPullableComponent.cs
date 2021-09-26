using System;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Pulling.Components
{
    [NetworkedComponent()]
    public abstract class SharedPullableComponent : Component
    {
        public override string Name => "Pullable";

        [ComponentDependency] private readonly PhysicsComponent? _physics = default!;

        /// <summary>
        /// Only set in Puller->set! Only set in unison with _pullerPhysics!
        /// </summary>
        private IEntity? _puller;

        public IPhysBody? PullerPhysics { get; private set; }

        private DistanceJoint? _pullJoint;

        public float? MaxDistance => _pullJoint?.MaxLength;

        private MapCoordinates? _movingTo;

        /// <summary>
        /// The current entity pulling this component.
        /// Setting this performs the entire setup process for pulling.
        /// </summary>
        public virtual IEntity? Puller
        {
            get => _puller;
            set
            {
                if (_puller == value)
                {
                    return;
                }

                var eventBus = Owner.EntityManager.EventBus;
                // TODO: JESUS

                // New value. Abandon being pulled by any existing object.
                if (_puller != null)
                {
                    var oldPuller = _puller;
                    var oldPullerPhysics = PullerPhysics;

                    if (_puller.TryGetComponent(out SharedPullerComponent? puller))
                    {
                        puller.Pulling = null;
                    }

                    _puller = null;
                    Dirty();
                    PullerPhysics = null;

                    if (_physics != null && oldPullerPhysics != null)
                    {
                        var message = new PullStoppedMessage(oldPullerPhysics, _physics);

                        eventBus.RaiseLocalEvent(oldPuller.Uid, message, broadcast: false);
                        eventBus.RaiseLocalEvent(Owner.Uid, message);

                        _physics.WakeBody();
                    }

                    // else-branch warning is handled below
                }

                // Now that is settled, prepare to be pulled by a new object.
                if (_physics == null)
                {
                    Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? SharedPullableComponent on {0} didn't have an IPhysBody.", Owner);
                    return;
                }

                if (value == null)
                {
                    MovingTo = null;
                }
                else
                {
                    // Pulling a new object : Perform sanity checks.

                    if (!_canStartPull(value))
                    {
                        return;
                    }

                    if (!value.TryGetComponent<PhysicsComponent>(out var pullerPhysics))
                    {
                        return;
                    }

                    if (!value.TryGetComponent<SharedPullerComponent>(out var valuePuller))
                    {
                        return;
                    }

                    // Ensure that the puller is not currently pulling anything.
                    // If this isn't done, then it happens too late, and the start/stop messages go out of order,
                    //  and next thing you know it thinks it's not pulling anything even though it is!

                    var oldPulling = valuePuller.Pulling;
                    if (oldPulling != null)
                    {
                        if (oldPulling.TryGetComponent(out SharedPullableComponent? pullable))
                        {
                            pullable.TryStopPull();
                        }
                        else
                        {
                            Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? Someone transferred pulling to this component (on {0}) while presently pulling something that has no Pullable component (on {1})!", Owner, oldPulling);
                            return;
                        }
                    }

                    valuePuller.Pulling = Owner;

                    // Continue with pulling process.

                    var pullAttempt = new PullAttemptMessage(pullerPhysics, _physics);

                    eventBus.RaiseLocalEvent(value.Uid, pullAttempt, broadcast: false);

                    if (pullAttempt.Cancelled)
                    {
                        return;
                    }

                    eventBus.RaiseLocalEvent(Owner.Uid, pullAttempt);

                    if (pullAttempt.Cancelled)
                    {
                        return;
                    }

                    // Pull start confirm

                    _puller = value;
                    Dirty();
                    PullerPhysics = pullerPhysics;

                    var message = new PullStartedMessage(PullerPhysics, _physics);

                    eventBus.RaiseLocalEvent(_puller.Uid, message, broadcast: false);
                    eventBus.RaiseLocalEvent(Owner.Uid, message);

                    var union = PullerPhysics.GetWorldAABB().Union(_physics.GetWorldAABB());
                    var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                    _physics.WakeBody();
                    _pullJoint = pullerPhysics.CreateDistanceJoint(_physics, $"pull-joint-{_physics.Owner.Uid}");
                    // _physics.BodyType = BodyType.Kinematic; // TODO: Need to consider their original bodytype
                    _pullJoint.CollideConnected = false;
                    _pullJoint.Length = length * 0.75f;
                    _pullJoint.MaxLength = length;
                }
                // Code here will not run if pulling a new object was attempted and failed because of the returns from the refactor.
            }
        }

        public bool BeingPulled => Puller != null;

        public MapCoordinates? MovingTo
        {
            get => _movingTo;
            set
            {
                if (_movingTo == value)
                {
                    return;
                }

                _movingTo = value;

                if (value == null)
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PullableStopMovingMessage());
                }
                else
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PullableMoveMessage());
                }
            }
        }

        /// <summary>
        /// Sanity-check pull. This is called from Puller setter, so it will never deny a pull that's valid by setting Puller.
        /// It might allow an impossible pull (i.e: puller has no PhysicsComponent somehow).
        /// Ultimately this is only used separately to stop TryStartPull from cancelling a pull for no reason.
        /// </summary>
        private bool _canStartPull(IEntity puller)
        {
            if (!puller.HasComponent<SharedPullerComponent>())
            {
                return false;
            }

            if (!EntitySystem.Get<SharedPullingSystem>().CanPull(puller, Owner))
            {
                return false;
            }

            if (_physics == null)
            {
                return false;
            }

            if (_physics.BodyType == BodyType.Static)
            {
                return false;
            }

            if (puller == Owner)
            {
                return false;
            }

            if (!puller.IsInSameOrNoContainer(Owner))
            {
                return false;
            }

            return true;
        }

        public bool TryStartPull(IEntity puller)
        {
            if (!_canStartPull(puller))
            {
                return false;
            }

            TryStopPull();

            Puller = puller;

            if(Puller != puller)
            {
                return false;
            }

            return true;
        }

        public bool TryStopPull(IEntity? user = null)
        {
            if (!BeingPulled)
            {
                return false;
            }

            var msg = new StopPullingEvent(user?.Uid);
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, msg);

            if (msg.Cancelled) return false;

            if (_physics != null && _pullJoint != null)
            {
                _physics.RemoveJoint(_pullJoint);
            }

            if (user != null && user.TryGetComponent<SharedPullerComponent>(out var puller))
            {
                puller.Pulling = null;
            }

            _pullJoint = null;
            Puller = null;
            return true;
        }

        public bool TogglePull(IEntity puller)
        {
            if (BeingPulled)
            {
                if (Puller == puller)
                {
                    return TryStopPull();
                }
                else
                {
                    TryStopPull();
                    return TryStartPull(puller);
                }
            }

            return TryStartPull(puller);
        }

        public bool TryMoveTo(MapCoordinates to)
        {
            if (Puller == null)
            {
                return false;
            }

            if (_physics == null)
            {
                return false;
            }

            MovingTo = to;
            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PullableComponentState(Puller?.Uid);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not PullableComponentState state)
            {
                return;
            }

            if (state.Puller == null)
            {
                Puller = null;
                return;
            }

            if (!Owner.EntityManager.TryGetEntity(state.Puller.Value, out var entity))
            {
                Logger.Error($"Invalid entity {state.Puller.Value} for pulling");
                return;
            }

            Puller = entity;
        }

        protected override void OnRemove()
        {
            TryStopPull();
            MovingTo = null;

            base.OnRemove();
        }
    }

    [Serializable, NetSerializable]
    public class PullableComponentState : ComponentState
    {
        public readonly EntityUid? Puller;

        public PullableComponentState(EntityUid? puller)
        {
            Puller = puller;
        }
    }

    /// <summary>
    /// Raised when a request is made to stop pulling an entity.
    /// </summary>
    public sealed class StopPullingEvent : CancellableEntityEventArgs
    {
        public EntityUid? User { get; }

        public StopPullingEvent(EntityUid? uid = null)
        {
            User = uid;
        }
    }
}
