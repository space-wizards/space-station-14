using Content.Client.Disposal.Components;
using Content.Client.Disposal.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Initializes a <see cref="DisposalUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        public DisposalUnitWindow? Window;

        public DisposalUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
            // If we get client-side power stuff then we can predict the button presses but for now we won't as it stuffs
            // the pressure lerp up.
        }

        protected override void Open()
        {
            base.Open();

            Window = new DisposalUnitWindow();

            Window.OpenCentered();
            Window.OnClose += Close;

            Window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            Window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            Window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalUnitBoundUserInterfaceState cast)
            {
                return;
            }

            Window?.UpdateState(cast);

            // Kinda icky but we just want client to handle its own lerping and not flood bandwidth for it.
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Owner, out DisposalUnitComponent? component)) return;

            component.UiState = cast;
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
