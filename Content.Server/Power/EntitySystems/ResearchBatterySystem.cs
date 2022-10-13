using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Shared.Damage;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ResearchBatterySystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const float UpdateRate = 10f;
        private float _updateDif;

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
                if ((component.analysedCharge + ((batteryComponent.CurrentCharge - component.lastRecordedCharge) * component.analysisSiphon)) <= component.MaxAnalysisCharge)
                {
                    component.analysedCharge += (batteryComponent.CurrentCharge - component.lastRecordedCharge) * component.analysisSiphon;

                    //ideally the charge should be taken away, but this appears to cause a crash...
                    //batteryComponent.CurrentCharge -= (batteryComponent.CurrentCharge - component.lastRecordedCharge) * component.analysisSiphon;
                }
                else if ((component.analysedCharge + ((batteryComponent.CurrentCharge - component.lastRecordedCharge) * component.analysisSiphon)) > component.MaxAnalysisCharge
                    && component.analysedCharge < component.MaxAnalysisCharge)
                {
                    //batteryComponent.CurrentCharge -= (1000000f - component.analysedCharge);
                    component.analysedCharge = 1000000f;
                }
            }

            component.lastRecordedCharge = batteryComponent.CurrentCharge;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // check update rate
            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif = 0f;

            var researchBatteries = EntityManager.EntityQuery<ResearchBatteryComponent>();

            foreach (var researchBattery in researchBatteries)
            {
                if (researchBattery.researchMode && researchBattery.analysedCharge > 0) {
                    
                    if (!TryComp<BatteryComponent>(researchBattery.Owner, out var batteryComponent))
                        return;

                    if ((batteryComponent.MaxCharge + (researchBattery.analysedCharge * researchBattery.CapIncrease)) <= researchBattery.MaxChargeCeiling)
                    {
                        batteryComponent.MaxCharge += researchBattery.analysedCharge * researchBattery.CapIncrease;
                        //Console.WriteLine("max charge added");
                    }
                    else if ((batteryComponent.MaxCharge + (researchBattery.analysedCharge * researchBattery.CapIncrease)) > researchBattery.MaxChargeCeiling
                        && batteryComponent.MaxCharge < researchBattery.MaxChargeCeiling)
                    {
                        batteryComponent.MaxCharge = 100000000f;
                        //Console.WriteLine("max charge reached");
                    }

                    if (researchBattery.shieldingActive && researchBattery.analysedCharge >= researchBattery.MaxAnalysisCharge * researchBattery.shieldingCost)
                        researchBattery.analysedCharge -= researchBattery.MaxAnalysisCharge * researchBattery.shieldingCost;
                    else if (researchBattery.shieldingActive)
                    {
                        researchBattery.shieldingActive = false;
                        researchBattery.analysedCharge = 0f;
                    }

                    if (!researchBattery.shieldingActive && researchBattery.analysedCharge > researchBattery.MaxAnalysisCharge * researchBattery.overloadThreshold)
                    {
                        EntitySystem.Get<DamageableSystem>().TryChangeDamage(researchBattery.Owner, researchBattery.Damage * (researchBattery.analysedCharge - researchBattery.MaxAnalysisCharge * researchBattery.overloadThreshold) / 10000, true);
                    }

                    researchBattery.analysedCharge -= researchBattery.analysedCharge * researchBattery.AnalysisDischarge;

                    researchBattery.UpdateUserInterface();

                }
            }
        }
    }
}
