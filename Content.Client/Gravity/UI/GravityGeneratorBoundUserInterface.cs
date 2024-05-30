using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

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

            _window = this.CreateWindow<GravityGeneratorWindow>();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (SharedGravityGeneratorComponent.GeneratorState) state;
            _window?.UpdateState(castState);
        }

        public void SetPowerSwitch(bool on)
        {
            SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(on));
        }
    }
}
