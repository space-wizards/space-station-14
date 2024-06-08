using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ParticleAccelerator.UI
{
    public sealed class ParticleAcceleratorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ParticleAcceleratorControlMenu? _menu;

        public ParticleAcceleratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<ParticleAcceleratorControlMenu>();
            _menu.OnOverallState += SendEnableMessage;
            _menu.OnPowerState += SendPowerStateMessage;
            _menu.OnScanPartsRequested += SendScanPartsMessage;
        }

        public void SendEnableMessage(bool enable)
        {
            SendMessage(new ParticleAcceleratorSetEnableMessage(enable));
        }

        public void SendPowerStateMessage(ParticleAcceleratorPowerState state)
        {
            SendMessage(new ParticleAcceleratorSetPowerStateMessage(state));
        }

        public void SendScanPartsMessage()
        {
            SendMessage(new ParticleAcceleratorRescanPartsMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            _menu?.DataUpdate((ParticleAcceleratorUIState) state);
        }
    }
}
