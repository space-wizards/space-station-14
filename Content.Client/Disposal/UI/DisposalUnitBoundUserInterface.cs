using Content.Client.Disposal.Components;
using Content.Client.Disposal.Systems;
using Content.Shared.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public MailingUnitWindow? MailingUnitWindow;
        public DisposalUnitWindow? DisposalUnitWindow;

        public DisposalUnitBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
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
            else if(UiKey is DisposalUnitUiKey)
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

            var entityId = Owner.Owner;
            if (!_entityManager.TryGetComponent(entityId, out DisposalUnitComponent? component))
                return;

            switch (state)
            {
                case MailingUnitBoundUserInterfaceState mailingUnitState:
                    MailingUnitWindow?.UpdateState(mailingUnitState);
                    component.UiState = mailingUnitState.DisposalState;
                    break;

                case DisposalUnitBoundUserInterfaceState disposalUnitState:
                    DisposalUnitWindow?.UpdateState(disposalUnitState);
                    component.UiState = disposalUnitState;
                    break;
            }

            _entityManager.System<DisposalUnitSystem>().UpdateActive(entityId, true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            MailingUnitWindow?.Dispose();
            DisposalUnitWindow?.Dispose();
        }

        public bool? UpdateWindowState(DisposalUnitBoundUserInterfaceState state)
        {
            return UiKey is DisposalUnitUiKey
                ? DisposalUnitWindow?.UpdateState(state)
                : MailingUnitWindow?.UpdatePressure(state.FullPressureTime);
        }
    }
}
