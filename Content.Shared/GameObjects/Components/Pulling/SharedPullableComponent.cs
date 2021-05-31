#nullable enable
using System;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystemMessages.Pulling;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Pulling
{
    public abstract class SharedPullableComponent : Component, IRelayMoveInput
    {
        public override string Name => "Pullable";
        public override uint? NetID => ContentNetIDs.PULLABLE;

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

                // New value. Abandon being pulled by any existing object.
                if (_puller != null)
                {
                    var oldPuller = _puller;
                    var oldPullerPhysics = PullerPhysics;

                    _puller = null;
                    Dirty();
                    PullerPhysics = null;

                    if (_physics != null && oldPullerPhysics != null)
                    {
                        var message = new PullStoppedMessage(oldPullerPhysics, _physics);

                        oldPuller.SendMessage(null, message);
                        Owner.SendMessage(null, message);

                        oldPuller.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
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

                    // Continue with pulling process.

                    var pullAttempt = new PullAttemptMessage(pullerPhysics, _physics);

                    value.SendMessage(null, pullAttempt);

                    if (pullAttempt.Cancelled)
                    {
                        return;
                    }

                    Owner.SendMessage(null, pullAttempt);

                    if (pullAttempt.Cancelled)
                    {
                        return;
                    }

                    // Pull start confirm

                    _puller = value;
                    Dirty();
                    PullerPhysics = pullerPhysics;

                    var message = new PullStartedMessage(PullerPhysics, _physics);

                    _puller.SendMessage(null, message);
                    Owner.SendMessage(null, message);

                    _puller.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

                    var union = PullerPhysics.GetWorldAABB().Union(_physics.GetWorldAABB());
                    var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                    _physics.WakeBody();
                    _pullJoint = pullerPhysics.CreateDistanceJoint(_physics);
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

            if (Puller != puller)
            {
                return false;
            }

            return true;
        }

        public bool TryStopPull()
        {
            if (!BeingPulled)
            {
                return false;
            }

            if (_physics != null && _pullJoint != null)
            {
                _physics.RemoveJoint(_pullJoint);
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

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (message is not PullMessage pullMessage ||
                pullMessage.Pulled.Owner != Owner)
            {
                return;
            }

            var pulledStatus = Owner.GetComponentOrNull<SharedAlertsComponent>();

            switch (message)
            {
                case PullStartedMessage:
                    pulledStatus?.ShowAlert(AlertType.Pulled);
                    break;
                case PullStoppedMessage:
                    pulledStatus?.ClearAlert(AlertType.Pulled);
                    break;
            }
        }

        public override void OnRemove()
        {
            TryStopPull();
            MovingTo = null;

            base.OnRemove();
        }

        // TODO: Need a component bus relay so all entities can use this and not just players
        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            var entity = session.AttachedEntity;
            if (entity == null || !ActionBlockerSystem.CanMove(entity)) return;
            TryStopPull();
        }
    }

    [Serializable, NetSerializable]
    public class PullableComponentState : ComponentState
    {
        public readonly EntityUid? Puller;

        public PullableComponentState(EntityUid? puller) : base(ContentNetIDs.PULLABLE)
        {
            Puller = puller;
        }
    }
}
