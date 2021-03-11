using Content.Shared.GameObjects.Components.Body.Surgery;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Surgery
{
    // TODO BODY Make window close if target or surgery tool gets too far away from user.

    /// <summary>
    ///     Generic client-side UI list popup that allows users to choose from an option
    ///     of limbs or organs to operate on.
    /// </summary>
    [UsedImplicitly]
    public class SurgeryBoundUserInterface : BoundUserInterface
    {
        private SurgeryWindow? _window;

        public SurgeryBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            _window = new SurgeryWindow();

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case RequestBodyPartSurgeryUIMessage msg:
                    HandleBodyPartRequest(msg);
                    break;
                case RequestMechanismSurgeryUIMessage msg:
                    HandleMechanismRequest(msg);
                    break;
                case RequestBodyPartSlotSurgeryUIMessage msg:
                    HandleBodyPartSlotRequest(msg);
                    break;
            }
        }

        private void HandleBodyPartRequest(RequestBodyPartSurgeryUIMessage msg)
        {
            _window?.BuildDisplay(msg.Targets, BodyPartSelectedCallback);
        }

        private void HandleMechanismRequest(RequestMechanismSurgeryUIMessage msg)
        {
            _window?.BuildDisplay(msg.Targets, MechanismSelectedCallback);
        }

        private void HandleBodyPartSlotRequest(RequestBodyPartSlotSurgeryUIMessage msg)
        {
            _window?.BuildDisplay(msg.Targets, BodyPartSlotSelectedCallback);
        }

        private void BodyPartSelectedCallback(int selectedOptionData)
        {
            SendMessage(new ReceiveBodyPartSurgeryUIMessage(selectedOptionData));
        }

        private void MechanismSelectedCallback(int selectedOptionData)
        {
            SendMessage(new ReceiveMechanismSurgeryUIMessage(selectedOptionData));
        }

        private void BodyPartSlotSelectedCallback(int selectedOptionData)
        {
            SendMessage(new ReceiveBodyPartSlotSurgeryUIMessage(selectedOptionData));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
