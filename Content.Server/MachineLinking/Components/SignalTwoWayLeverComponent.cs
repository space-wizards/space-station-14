#nullable enable
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Notification;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class SignalTwoWayLeverComponent : SignalTransmitterComponent, IInteractHand, IActivate
    {
        public override string Name => "TwoWayLever";

        private TwoWayLeverSignal _state = TwoWayLeverSignal.Middle;

        private bool _nextForward = true;

        public TwoWayLeverSignal State
        {
            get => _state;
            private set
            {
                _state = value;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(TwoWayLeverVisuals.State, value);
                }
            }
        }

        private void NextState(IEntity user)
        {
            State = State switch
            {
                TwoWayLeverSignal.Left => TwoWayLeverSignal.Middle,
                TwoWayLeverSignal.Middle => _nextForward ? TwoWayLeverSignal.Right : TwoWayLeverSignal.Left,
                TwoWayLeverSignal.Right => TwoWayLeverSignal.Middle,
                _ => TwoWayLeverSignal.Middle
            };

            if (State == TwoWayLeverSignal.Left || State == TwoWayLeverSignal.Right) _nextForward = !_nextForward;

            if (!TransmitSignal(State))
            {
                Owner.PopupMessage(user, Loc.GetString("No receivers connected."));
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            NextState(eventArgs.User);
            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            NextState(eventArgs.User);
        }
    }
}
