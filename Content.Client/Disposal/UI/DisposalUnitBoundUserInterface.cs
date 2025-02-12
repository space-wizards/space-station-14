using Content.Shared.Disposal;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Initializes a <see cref="MailingUnitWindow"/> or a <see cref="DisposalUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        // What are you doing here
        [ViewVariables]
        public MailingUnitWindow? MailingUnitWindow;

        [ViewVariables]
        public DisposalUnitWindow? DisposalUnitWindow;

        public DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
            // If we get client-side power stuff then we can predict the button presses but for now we won't as it stuffs
            // the pressure lerp up.
        }

        private void TargetSelected(ItemList.ItemListSelectedEventArgs args)
        {
            var item = args.ItemList[args.ItemIndex];
            SendMessage(new TargetSelectedMessage(item.Text));
        }

        protected override void Open()
        {
            base.Open();

            if (UiKey is MailingUnitUiKey)
            {
                MailingUnitWindow = new MailingUnitWindow();

                MailingUnitWindow.OpenCenteredRight();
                MailingUnitWindow.OnClose += Close;

                MailingUnitWindow.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
                MailingUnitWindow.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
                MailingUnitWindow.Power.OnPressed += _ => ButtonPressed(UiButton.Power);

                MailingUnitWindow.TargetListContainer.OnItemSelected += TargetSelected;
            }
            else if (UiKey is DisposalUnitUiKey)
            {
                DisposalUnitWindow = new DisposalUnitWindow();

                DisposalUnitWindow.OpenCenteredRight();
                DisposalUnitWindow.OnClose += Close;

                DisposalUnitWindow.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
                DisposalUnitWindow.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
                DisposalUnitWindow.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
            }
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MailingUnitBoundUserInterfaceState && state is not DisposalUnitBoundUserInterfaceState)
            {
                return;
            }

            switch (state)
            {
                case MailingUnitBoundUserInterfaceState mailingUnitState:
                    MailingUnitWindow?.UpdateState(mailingUnitState);
                    break;

                case DisposalUnitBoundUserInterfaceState disposalUnitState:
                    DisposalUnitWindow?.UpdateState(disposalUnitState);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            MailingUnitWindow?.Dispose();
            DisposalUnitWindow?.Dispose();
        }
    }
}
