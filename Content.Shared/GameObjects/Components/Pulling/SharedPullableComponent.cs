#nullable enable
using System;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Pulling
{
    public abstract class SharedPullableComponent : Component, ICollideSpecial
    {
        public override string Name => "Pullable";
        public override uint? NetID => ContentNetIDs.PULLABLE;

        [ComponentDependency] private readonly IPhysicsComponent? _physics = default!;

        /// <summary>
        /// Only set in Puller->set! Only set in unison with _pullerPhysics!
        /// </summary>
        private IEntity? _puller;
        private IPhysicsComponent? _pullerPhysics;
        public IPhysicsComponent? PullerPhysics => _pullerPhysics;

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
                    var oldPullerPhysics = _pullerPhysics;

                    _puller = null;
                    _pullerPhysics = null;

                    if (_physics != null)
                    {
                        var message = new PullStoppedMessage(oldPullerPhysics, _physics);

                        oldPuller.SendMessage(null, message);
                        Owner.SendMessage(null, message);

                        oldPuller.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
                        _physics.WakeBody();
                        _physics.TryRemoveController<PullController>();
                    }
                    // else-branch warning is handled below
                }

                // Now that is settled, prepare to be pulled by a new object.
                if (_physics == null)
                {
                    Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? SharedPullableComponent on {0} didn't have an IPhysicsComponent.", Owner);
                    return;
                }

                if (value != null)
                {
                    // Pulling a new object : Perform sanity checks.

                    if (!_canStartPull(value))
                    {
                        return;
                    }

                    if (!value.TryGetComponent<IPhysicsComponent>(out var valuePhysics))
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
                            pullable.Puller = null;
                        }
                        else
                        {
                            Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? Someone transferred pulling to this component (on {0}) while presently pulling something that has no Pullable component (on {1})!", Owner, oldPulling);
                            return;
                        }
                    }

                    // Continue with pulling process.

                    var pullAttempt = new PullAttemptMessage(valuePhysics, _physics);

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
                    _pullerPhysics = valuePhysics;

                    _physics.EnsureController<PullController>().Manager = this;
                    var message = new PullStartedMessage(_pullerPhysics, _physics);

                    _puller.SendMessage(null, message);
                    Owner.SendMessage(null, message);

                    _puller.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);

                    _physics.WakeBody();
                }
                // Code here will not run if pulling a new object was attempted and failed because of the returns from the refactor.
            }
        }

        public bool BeingPulled => Puller != null;

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

            if (_physics.Anchored)
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

        public bool TryMoveTo(EntityCoordinates to)
        {
            if (Puller == null)
            {
                return false;
            }

            if (_physics == null)
            {
                return false;
            }

            if (!_physics.TryGetController(out PullController controller))
            {
                return false;
            }

            return controller.TryMoveTo(Puller.Transform.Coordinates, to);
        }

        public override ComponentState GetComponentState()
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

            SharedAlertsComponent? pulledStatus = Owner.GetComponentOrNull<SharedAlertsComponent>();

            switch (message)
            {
                case PullStartedMessage msg:
                    if (pulledStatus != null)
                    {
                        pulledStatus.ShowAlert(AlertType.Pulled);
                    }
                    break;
                case PullStoppedMessage msg:
                    if (pulledStatus != null)
                    {
                        pulledStatus.ClearAlert(AlertType.Pulled);
                    }
                    break;
            }
        }

        private void OnClickAlert(ClickAlertEventArgs args)
        {
            EntitySystem
                .Get<SharedPullingSystem>()
                .GetPulled(args.Player)?
                .GetComponentOrNull<SharedPullableComponent>()?
                .TryStopPull();
        }

        public override void OnRemove()
        {
            TryStopPull();

            base.OnRemove();
        }

        public bool PreventCollide(IPhysBody collidedWith)
        {
            if (_puller == null || _physics == null)
            {
                return false;
            }

            return (_physics.CollisionLayer & collidedWith.CollisionMask) == (int) CollisionGroup.MobImpassable;
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
