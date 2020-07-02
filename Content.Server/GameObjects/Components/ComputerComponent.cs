using Content.Server.GameObjects.Components.Power.ApcNetComponents;
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

            if (Owner.TryGetComponent(out PowerReceiverComponent powerReceiver))
            {
                powerReceiver.OnPowerStateChanged += PowerReceiverOnOnPowerStateChanged;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ComputerVisuals.Powered, powerReceiver.Powered);
                }
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent powerReceiver))
            {
                powerReceiver.OnPowerStateChanged -= PowerReceiverOnOnPowerStateChanged;
            }

            base.OnRemove();
        }

        private void PowerReceiverOnOnPowerStateChanged(object sender, PowerStateEventArgs e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(ComputerVisuals.Powered, e.Powered);
            }
        }
    }
}
