using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.UserInterface.Systems.Atmos.GasTank
{
    [UsedImplicitly]
    public sealed class GasTankBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GasTankWindow? _window;

        public GasTankBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        public void SetOutputPressure(in float value)
        {
            SendMessage(new GasTankSetPressureMessage
            {
                Pressure = value
            });
        }

        public void ToggleInternals()
        {
            SendMessage(new GasTankToggleInternalsMessage());
        }

        protected override void Open()
        {
            base.Open();
            _window = new GasTankWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is GasTankBoundUserInterfaceState cast)
                _window?.UpdateState(cast);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _window?.Close();
        }
    }
}
