#nullable enable
using System;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
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

        private IEntity? _puller;

        public virtual IEntity? Puller
        {
            get => _puller;
            private set
            {
                if (_puller == value)
                {
                    return;
                }

                _puller = value;
                Dirty();

                if (_physics == null)
                {
                    return;
                }

                PullController controller;

                if (value == null)
                {
                    if (_physics.TryGetController(out controller))
                    {
                        controller.StopPull();
                    }

                    return;
                }

                controller = _physics.EnsureController<PullController>();
                controller.StartPull(value);
            }
        }

        public bool BeingPulled => Puller != null;

        public bool CanStartPull(IEntity puller)
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
            if (!CanStartPull(puller))
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

            switch (message)
            {
                case PullStartedMessage msg:
                    AddPullingStatuses(msg.Puller.Owner);
                    break;
                case PullStoppedMessage msg:
                    RemovePullingStatuses(msg.Puller.Owner);
                    break;
            }
        }

        private void AddPullingStatuses(IEntity puller)
        {
            if (Owner.TryGetComponent(out SharedAlertsComponent? pulledStatus))
            {
                pulledStatus.ShowAlert(AlertType.Pulled);
            }

            if (puller.TryGetComponent(out SharedAlertsComponent? ownerStatus))
            {
                ownerStatus.ShowAlert(AlertType.Pulling, onClickAlert: OnClickAlert);
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

        private void RemovePullingStatuses(IEntity puller)
        {
            if (Owner.TryGetComponent(out SharedAlertsComponent? pulledStatus))
            {
                pulledStatus.ClearAlert(AlertType.Pulled);
            }

            if (puller.TryGetComponent(out SharedAlertsComponent? ownerStatus))
            {
                ownerStatus.ClearAlert(AlertType.Pulling);
            }
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
