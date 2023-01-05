using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="CentrifugeWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class CentrifugeBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private CentrifugeWindow? _window;

        public CentrifugeBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        /// <summary>
        /// Called each time a UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = new CentrifugeWindow
            {
                Title = _entityManager.GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            // Setup static button actions.
            _window.InputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(SharedCentrifuge.InputSlotName));
            _window.OutputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(SharedCentrifuge.OutputSlotName));

            _window.BufferTransferButton.OnPressed += _ => SendMessage(
                new CentrifugeSetModeMessage(CentrifugeMode.Transfer));

            _window.BufferDiscardButton.OnPressed += _ => SendMessage(
                new CentrifugeSetModeMessage(CentrifugeMode.Discard));

            _window.ActivateButton.OnPressed += _ => SendMessage(
                new CentrifugeActivateButtonMessage(true));

            _window.ElectrolysisButton.OnPressed += _ => SendMessage(
                new CentrifugeElectrolysisButtonMessage(true));

            _window.OnCentrifugeReagentButtonPressed += (args, button) => SendMessage(new CentrifugeReagentAmountButtonMessage(button.Id, button.Amount, button.IsBuffer));
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (CentrifugeBoundUserInterfaceState) state;

            _window?.UpdateState(castState); // Update window state
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
