#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Chemistry.ChemMaster;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Chemistry.SharedChemMasterComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class ChemMasterBoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager = default!;
#pragma warning restore 649

        private ChemMasterWindow? _window;

        public ChemMasterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            //Setup window layout/elements
            _window = new ChemMasterWindow
            {
                Title = _localizationManager.GetString("ChemMaster 4000"),
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            //Setup static button actions.
            _window.EjectButton.OnPressed += _ => PrepareData(UiAction.Eject, null, null, null);
            _window.BufferTransferButton.OnPressed += _ => PrepareData(UiAction.Transfer, null, null, null);
            _window.BufferDiscardButton.OnPressed += _ => PrepareData(UiAction.Discard, null, null, null);
            _window.CreatePills.OnPressed += _ => PrepareData(UiAction.CreatePills, null, _window.PillAmount.Value, null);
            _window.CreateBottles.OnPressed += _ => PrepareData(UiAction.CreateBottles, null, null, _window.BottleAmount.Value);

            _window.OnChemButtonPressed += (args, button) => PrepareData(UiAction.ChemButton, button, null, null);
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ChemMasterBoundUserInterfaceState)state;

            _window?.UpdateState(castState); //Update window state
        }

        private void PrepareData(UiAction action, ChemButton? button, int? pillAmount, int? bottleAmount)
        {
            if (button != null)
            {
                SendMessage(new UiActionMessage(action, button.Amount, button.Id, button.isBuffer, null, null));
            }
            else
            {
                SendMessage(new UiActionMessage(action, null, null, null, pillAmount, bottleAmount));
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }
    }
}
