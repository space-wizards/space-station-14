using Content.Shared.GameObjects.Components.Medical;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent;

namespace Content.Client.GameObjects.Components.CloningMachine
{
    [UsedImplicitly]
    public class CloningMachineBoundUserInterface : BoundUserInterface
    {
        public CloningMachineBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private CloningMachineWindow _window;

        protected override void Open()
        {
            base.Open();
            _window = new CloningMachineWindow
            {
                Title = Owner.Owner.Name,
            };
            _window.OnClose += Close;
            _window.ScanButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window.Populate((CloningMachineBoundUserInterfaceState) state);
        }
    }
}
