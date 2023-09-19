using Content.Server.Construction;
using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class UpgradeBatterySystem : EntitySystem
    {
        [Dependency] private readonly BatterySystem _batterySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UpgradeBatteryComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<UpgradeBatteryComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        }

        public void OnRefreshParts(EntityUid uid, UpgradeBatteryComponent component, RefreshPartsEvent args)
        {
            var powerCellRating = args.PartRatings[component.MachinePartPowerCapacity];

            if (TryComp<BatteryComponent>(uid, out var batteryComp))
            {
                _batterySystem.SetMaxCharge(uid, MathF.Pow(component.MaxChargeMultiplier, powerCellRating - 1) * component.BaseMaxCharge, batteryComp);
            }
        }

        private void OnUpgradeExamine(EntityUid uid, UpgradeBatteryComponent component, UpgradeExamineEvent args)
        {
            // UpgradeBatteryComponent.MaxChargeMultiplier is not the actual multiplier, so we have to do this.
            if (TryComp<BatteryComponent>(uid, out var batteryComp))
            {
                args.AddPercentageUpgrade("upgrade-max-charge", batteryComp.MaxCharge / component.BaseMaxCharge);
            }
        }
    }
}
