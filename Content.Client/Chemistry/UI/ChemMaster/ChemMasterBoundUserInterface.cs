using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI.ChemMaster
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ChemMasterWindow? _window;

        public ChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = this.CreateWindow<ChemMasterWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // Setup static button actions.
            _window.OnReagentButton += args =>
                SendPredictedMessage(new ChemMasterReagentAmountButtonMessage(args.Id, args.Amount, args.IsBuffer));
            _window.OnModeButton += mode => SendPredictedMessage(new ChemMasterSetModeMessage(mode));
            _window.OnSortButton += () => SendPredictedMessage(new ChemMasterSortingTypeCycleMessage());
            _window.OnEjectButton += slot => SendPredictedMessage(new ItemSlotButtonPressedEvent(slot));

            _window.OnPillButton += index => SendPredictedMessage(new ChemMasterSetPillTypeMessage(index));
            _window.OnCreatePill += args =>
                SendPredictedMessage(new ChemMasterCreatePillsMessage(args.Dosage, args.Count, args.Label));
            _window.OnCreateBottle += args =>
                SendPredictedMessage(new ChemMasterOutputToBottleMessage(args.Dosage, args.Label));
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="ChemMasterComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ChemMasterBoundUserInterfaceState) state;

            _window?.UpdateState(castState); // Update window state
        }
    }
}
