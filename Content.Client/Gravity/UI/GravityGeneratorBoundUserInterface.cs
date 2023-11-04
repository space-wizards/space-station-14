using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Gravity.UI
{
    [UsedImplicitly]
    public sealed class GravityGeneratorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GravityGeneratorWindow? _window;

        public GravityGeneratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new GravityGeneratorWindow(this);

            /*
            _window.Switch.OnPressed += _ =>
            {
                SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(!IsOn));
            };
            */

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedGravityGeneratorComponent.GeneratorState) state;
            _window?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _window?.Dispose();
        }

        public void SetPowerSwitch(bool on)
        {
            SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(on));
        }
    }
}
