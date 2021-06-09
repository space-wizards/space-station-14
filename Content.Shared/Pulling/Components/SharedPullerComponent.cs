#nullable enable
using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Physics.Pull;
using Robust.Shared.GameObjects;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.Pulling.Components
{
    [RegisterComponent]
    public class SharedPullerComponent : Component, IMoveSpeedModifier
    {
        public override string Name => "Puller";

        private IEntity? _pulling;

        public float WalkSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        public float SprintSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        public IEntity? Pulling
        {
            get => _pulling;
            private set
            {
                if (_pulling == value)
                {
                    return;
                }

                _pulling = value;

                if (Owner.TryGetComponent(out MovementSpeedModifierComponent? speed))
                {
                    speed.RefreshMovementSpeedModifiers();
                }
            }
        }

        public override void OnRemove()
        {
            if (Pulling != null &&
                Pulling.TryGetComponent(out SharedPullableComponent? pullable))
            {
                pullable.TryStopPull();
            }

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (message is not PullMessage pullMessage ||
                pullMessage.Puller.Owner != Owner)
            {
                return;
            }

            SharedAlertsComponent? ownerStatus = Owner.GetComponentOrNull<SharedAlertsComponent>();

            switch (message)
            {
                case PullStartedMessage msg:
                    Pulling = msg.Pulled.Owner;
                    ownerStatus?.ShowAlert(AlertType.Pulling);
                    break;
                case PullStoppedMessage _:
                    Pulling = null;
                    ownerStatus?.ClearAlert(AlertType.Pulling);
                    break;
            }
        }
    }
}
