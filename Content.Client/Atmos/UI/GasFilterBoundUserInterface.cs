using System;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasFilterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class GasFilterBoundUserInterface : BoundUserInterface
    {

        private GasFilterWindow? _window;
        private const float MaxTransferRate = Atmospherics.MaxTransferRate;

        public GasFilterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();

            _window = new GasFilterWindow(atmosSystem.Gases);

            if(State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.FilterTransferRateChanged += OnFilterTransferRatePressed;
            _window.SelectGasPressed += OnSelectGasPressed;
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;
            SendMessage(new GasFilterToggleStatusMessage(_window.FilterStatus));
        }

        private void OnFilterTransferRatePressed(string value)
        {
            float rate = float.TryParse(value, out var parsed) ? parsed : 0f;
            if (rate > MaxTransferRate) rate = MaxTransferRate;

            SendMessage(new GasFilterChangeRateMessage(rate));
        }

        private void OnSelectGasPressed()
        {
            if (_window is null || _window.SelectedGas is null) return;
            if (!Int32.TryParse(_window.SelectedGas, out var gas)) return;
            SendMessage(new GasFilterSelectGasMessage(gas));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasFilterBoundUserInterfaceState cast)
                return;

            _window.Title = (cast.FilterLabel);
            _window.SetFilterStatus(cast.Enabled);
            _window.SetTransferRate(cast.TransferRate);
            if (cast.FilteredGas is not null)
            {
                var atmos = EntitySystem.Get<AtmosphereSystem>();
                var gas = atmos.GetGas((Gas) cast.FilteredGas);
                _window.SetGasFiltered(gas.ID, gas.Name);
            }
            else
            {
                _window.SetGasFiltered(null, "None");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
