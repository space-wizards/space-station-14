#nullable enable
using System;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Pulling
{
    public abstract class SharedPullableComponent : Component
    {
        public override string Name => "Pullable";
        public override uint? NetID => ContentNetIDs.PULLABLE;

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

                if (!Owner.TryGetComponent(out IPhysicsComponent? physics))
                {
                    return;
                }

                PullController controller;

                if (value == null)
                {
                    if (physics.TryGetController(out controller))
                    {
                        controller.StopPull();
                    }

                    return;
                }

                controller = physics.EnsureController<PullController>();
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

            if (!puller.TryGetComponent(out IPhysicsComponent? physics))
            {
                return false;
            }

            if (physics.Anchored)
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

            if (!Owner.TryGetComponent(out IPhysicsComponent? physics))
            {
                return false;
            }

            if (!physics.TryGetController(out PullController controller))
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

            if (!(curState is PullableComponentState state))
            {
                return;
            }

            if (state.Puller == null)
            {
                Puller = null;
                return;
            }

            Puller = Owner.EntityManager.GetEntity(state.Puller.Value);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (!(message is PullMessage pullMessage) ||
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
            if (Owner.TryGetComponent(out SharedStatusEffectsComponent? pulledStatus))
            {
                pulledStatus.ChangeStatusEffectIcon(StatusEffect.Pulled,
                    "/Textures/Interface/StatusEffects/Pull/pulled.png");
            }

            if (puller.TryGetComponent(out SharedStatusEffectsComponent? ownerStatus))
            {
                ownerStatus.ChangeStatusEffectIcon(StatusEffect.Pulling,
                    "/Textures/Interface/StatusEffects/Pull/pulling.png");
            }
        }

        private void RemovePullingStatuses(IEntity puller)
        {
            if (Owner.TryGetComponent(out SharedStatusEffectsComponent? pulledStatus))
            {
                pulledStatus.RemoveStatusEffect(StatusEffect.Pulled);
            }

            if (puller.TryGetComponent(out SharedStatusEffectsComponent? ownerStatus))
            {
                ownerStatus.RemoveStatusEffect(StatusEffect.Pulling);
            }
        }

        public override void OnRemove()
        {
            TryStopPull();

            base.OnRemove();
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
