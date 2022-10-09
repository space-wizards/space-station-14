using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ResearchBatterySystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchBatteryComponent, MapInitEvent>(OnBatteryInit);
            SubscribeLocalEvent<ResearchBatteryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        }

        //Initialise the research battery last recorded charge
        private void OnBatteryInit(EntityUid uid, ResearchBatteryComponent component, MapInitEvent args)
        {
            if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
                return;

            component.lastRecordedCharge = batteryComponent.CurrentCharge;

        }

        //Checks if the battery is on research mode - if it is then the charge is redirected for analysis
        private void OnBatteryChargeChanged(EntityUid uid, ResearchBatteryComponent component, ChargeChangedEvent args)
        {

            if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
                return;

            if (component.lastRecordedCharge < batteryComponent.CurrentCharge && component.researchMode)
            {
                component.analysedCharge += batteryComponent.CurrentCharge - component.lastRecordedCharge;
                batteryComponent.CurrentCharge = component.lastRecordedCharge;
            }

            component.lastRecordedCharge = batteryComponent.CurrentCharge;
        }

    }
}
