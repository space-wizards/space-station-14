using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

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

        public void SetOutputPressure(float value)
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
            _window = this.CreateWindow<GasTankWindow>();
            _window.SetTitle(EntMan.GetComponent<MetaDataComponent>(Owner).EntityName);
            _window.OnOutputPressure += SetOutputPressure;
            _window.OnToggleInternals += ToggleInternals;
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
