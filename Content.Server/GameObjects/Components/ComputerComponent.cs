using Content.Server.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public sealed class ComputerComponent : SharedComputerComponent
    {
        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out PowerDeviceComponent powerDevice))
            {
                powerDevice.OnPowerStateChanged += PowerDeviceOnOnPowerStateChanged;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ComputerVisuals.Powered, powerDevice.Powered);
                }
            }
        }

        private void PowerDeviceOnOnPowerStateChanged(object sender, PowerStateEventArgs e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(ComputerVisuals.Powered, e.Powered);
            }
        }
    }
}
