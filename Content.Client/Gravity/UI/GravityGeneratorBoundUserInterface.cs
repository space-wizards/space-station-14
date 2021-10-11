using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Gravity.UI
{
    [UsedImplicitly]
    public class GravityGeneratorBoundUserInterface : BoundUserInterface
    {
        private GravityGeneratorWindow? _window;

        public bool IsOn;

        public GravityGeneratorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base (owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            IsOn = false;

            _window = new GravityGeneratorWindow(this);

            _window.Switch.OnPressed += _ =>
            {
                SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(!IsOn));
            };

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedGravityGeneratorComponent.GeneratorState) state;
            IsOn = castState.On;
            _window?.UpdateButton();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _window?.Dispose();
        }
    }
}
