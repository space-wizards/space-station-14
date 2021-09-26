using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.Pulling.Components
{
    [RegisterComponent]
    public class SharedPullerComponent : Component, IMoveSpeedModifier
    {
        public override string Name => "Puller";

        private IEntity? _pulling;

        public float WalkSpeedModifier => _pulling == null ? 1.0f : 0.75f;

        public float SprintSpeedModifier => _pulling == null ? 1.0f : 0.75f;

        [ViewVariables]
        public IEntity? Pulling
        {
            get => _pulling;
            set
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

        protected override void OnRemove()
        {
            if (Pulling != null &&
                Pulling.TryGetComponent(out SharedPullableComponent? pullable))
            {
                pullable.TryStopPull();
            }

            base.OnRemove();
        }
    }
}
