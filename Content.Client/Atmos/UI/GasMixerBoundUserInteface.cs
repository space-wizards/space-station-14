using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasMixerWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasMixerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private const float MaxPressure = Atmospherics.MaxOutputPressure;

        [ViewVariables]
        private GasMixerWindow? _window;

        public GasMixerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new GasMixerWindow();

            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.MixerOutputPressureChanged += OnMixerOutputPressurePressed;
            _window.MixerNodePercentageChanged += OnMixerSetPercentagePressed;
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;
            SendMessage(new GasMixerToggleStatusMessage(_window.MixerStatus));
        }

        private void OnMixerOutputPressurePressed(string value)
        {
            var pressure = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
            if (pressure > MaxPressure)
                pressure = MaxPressure;

            SendMessage(new GasMixerChangeOutputPressureMessage(pressure));
        }

        private void OnMixerSetPercentagePressed(string value)
        {
            // We don't need to send both nodes because it's just 100.0f - node
            var node = UserInputParser.TryFloat(value, out var parsed) ? parsed : 1.0f;

            node = Math.Clamp(node, 0f, 100.0f);

            if (_window is not null)
                node = _window.NodeOneLastEdited ? node : 100.0f - node;

            SendMessage(new GasMixerChangeNodePercentageMessage(node));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasMixerBoundUserInterfaceState cast)
                return;

            _window.Title = (cast.MixerLabel);
            _window.SetMixerStatus(cast.Enabled);
            _window.SetOutputPressure(cast.OutputPressure);
            _window.SetNodePercentages(cast.NodeOne);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
