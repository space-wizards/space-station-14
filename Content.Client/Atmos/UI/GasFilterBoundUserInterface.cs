using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasFilterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasFilterBoundUserInterface : BoundUserInterface
    {

        private GasFilterWindow? _window;
        private const float MaxTransferRate = Atmospherics.MaxTransferRate;

        public GasFilterBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var atmosSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AtmosphereSystem>();

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
            if (_window is null) return;
            if (_window.SelectedGas is null)
            {
                SendMessage(new GasFilterSelectGasMessage(null));
            }
            else
            {
                if (!int.TryParse(_window.SelectedGas, out var gas)) return;
                SendMessage(new GasFilterSelectGasMessage(gas));
            }
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
                var atmos = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AtmosphereSystem>();
                var gas = atmos.GetGas((Gas) cast.FilteredGas);
                var gasName = Loc.GetString(gas.Name);
                _window.SetGasFiltered(gas.ID, gasName);
            }
            else
            {
                _window.SetGasFiltered(null, Loc.GetString("comp-gas-filter-ui-filter-gas-none"));
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
