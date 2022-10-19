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

            component.LastRecordedCharge = batteryComponent.CurrentCharge;
        }

        //Checks if the battery is on research mode - if it is then the charge is redirected for analysis
        private void OnBatteryChargeChanged(EntityUid uid, ResearchBatteryComponent component, ChargeChangedEvent args)
        {

            if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
                return;

            if (component.LastRecordedCharge < batteryComponent.CurrentCharge && component.ResearchMode)
            {
                if ((component.AnalysedCharge + ((batteryComponent.CurrentCharge - component.LastRecordedCharge) * component.AnalysisSiphon)) <= component.MaxAnalysisCharge)
                    component.AnalysedCharge += (batteryComponent.CurrentCharge - component.LastRecordedCharge) * component.AnalysisSiphon;
                else if ((component.AnalysedCharge + ((batteryComponent.CurrentCharge - component.LastRecordedCharge) * component.AnalysisSiphon)) > component.MaxAnalysisCharge
                    && component.AnalysedCharge < component.MaxAnalysisCharge)
                    component.AnalysedCharge = 1000000f;
            }

            component.LastRecordedCharge = batteryComponent.CurrentCharge;
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
                if (researchBattery.ResearchMode && researchBattery.AnalysedCharge > 0) {
                    
                    if (!TryComp<BatteryComponent>(researchBattery.Owner, out var batteryComponent))
                        return;

                    if (!researchBattery.MaxCapReached && (batteryComponent.MaxCharge + (researchBattery.AnalysedCharge * researchBattery.CapIncrease)) <= researchBattery.MaxChargeCeiling)
                    {
                        batteryComponent.MaxCharge += researchBattery.AnalysedCharge * researchBattery.CapIncrease;
                    }
                    else if (!researchBattery.MaxCapReached && (batteryComponent.MaxCharge + (researchBattery.AnalysedCharge * researchBattery.CapIncrease)) > researchBattery.MaxChargeCeiling
                        && batteryComponent.MaxCharge < researchBattery.MaxChargeCeiling)
                    {
                        batteryComponent.MaxCharge = 100000000f;
                        researchBattery.MaxCapReached = true;
                    }

                    if (batteryComponent.MaxCharge >= researchBattery.ResearchGoal && !researchBattery.ResearchAchieved)
                    {
                        researchBattery.ResearchAchieved = true;
                        Spawn(researchBattery.ResearchDiskReward, Transform(researchBattery.Owner).Coordinates);
                    }


                    if (researchBattery.ShieldingActive && researchBattery.AnalysedCharge >= researchBattery.MaxAnalysisCharge * researchBattery.ShieldingCost)
                        researchBattery.AnalysedCharge -= researchBattery.MaxAnalysisCharge * researchBattery.ShieldingCost;
                    else if (researchBattery.ShieldingActive)
                    {
                        researchBattery.ShieldingActive = false;
                        researchBattery.AnalysedCharge = 0f;
                    }

                    if (!researchBattery.ShieldingActive && researchBattery.AnalysedCharge > researchBattery.MaxAnalysisCharge * researchBattery.OverloadThreshold)
                    {
                        EntitySystem.Get<DamageableSystem>().TryChangeDamage(researchBattery.Owner, researchBattery.Damage * (researchBattery.AnalysedCharge - researchBattery.MaxAnalysisCharge * researchBattery.OverloadThreshold) / 10000, true);
                    }

                    researchBattery.AnalysedCharge -= researchBattery.AnalysedCharge * researchBattery.AnalysisDischarge;

                    researchBattery.UpdateUserInterface();

                }
            }
        }
    }
}
