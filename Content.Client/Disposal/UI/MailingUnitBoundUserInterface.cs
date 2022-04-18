using Content.Client.Disposal.Components;
using Content.Client.Disposal.Systems;
using Content.Shared.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Initializes a <see cref="MailingUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class MailingUnitBoundUserInterface : BoundUserInterface
    {
        public MailingUnitWindow? Window;

        public MailingUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
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

            Window = new MailingUnitWindow();

            Window.OpenCentered();
            Window.OnClose += Close;

            Window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            Window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            Window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);

            Window.TargetListContainer.OnItemSelected += TargetSelected;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MailingUnitBoundUserInterfaceState cast)
            {
                return;
            }

            Window?.UpdateState(cast);

            // Kinda icky but we just want client to handle its own lerping and not flood bandwidth for it.
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Owner, out DisposalUnitComponent? component)) return;

            component.UiState = cast.DisposalState;
            EntitySystem.Get<DisposalUnitSystem>().UpdateActive(component, true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Window?.Dispose();
            }
        }
    }
}
