using Content.Shared.GameObjects.Components.Power;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    public class ApcComponent : Component
    {
        public override string Name => "Apc";

        PowerStorageComponent Storage;
        AppearanceComponent Appearance;

        ApcChargeState LastChargeState;

        public override void Initialize()
        {
            base.Initialize();
            Storage = Owner.GetComponent<PowerStorageComponent>();
            Appearance = Owner.GetComponent<AppearanceComponent>();
        }

        public void OnUpdate()
        {
            var newState = CalcChargeState();
            if (newState != LastChargeState)
            {
                LastChargeState = newState;
                Appearance.SetData(ApcVisuals.ChargeState, newState);
            }
        }

        ApcChargeState CalcChargeState()
        {
            var storageCharge = Storage.GetChargeState();
            if (storageCharge == ChargeState.Discharging)
            {
                return ApcChargeState.Lack;
            }

            if (storageCharge == ChargeState.Charging)
            {
                return ApcChargeState.Charging;
            }

            // Still.
            return Storage.Full ? ApcChargeState.Full : ApcChargeState.Lack;
        }
    }
}
