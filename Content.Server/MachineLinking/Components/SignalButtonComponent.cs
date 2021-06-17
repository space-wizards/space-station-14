using Content.Server.MachineLinking.Signals;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalButtonComponent : Component, IActivate, IInteractHand
    {
        public override string Name => "SignalButton";

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            TransmitSignal(eventArgs.User);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            TransmitSignal(eventArgs.User);
            return true;
        }

        private void TransmitSignal(IEntity user)
        {
            if (!Owner.TryGetComponent<SignalTransmitterComponent>(out var transmitter))
            {
                return;
            }

            if (transmitter.TransmitSignal(new ToggleSignal()))
            {
                // Since the button doesn't have an animation, I'm going to use a popup message
                Owner.PopupMessage(user, Loc.GetString("Click."));
            }
            else
            {
                Owner.PopupMessage(user, Loc.GetString("No receivers connected."));
            }
        }

    }
}
