using Content.Server.Construction;
using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class UpgradeBatterySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UpgradeBatteryComponent, RefreshPartsEvent>(OnRefreshParts);
        }

        public void OnRefreshParts(EntityUid uid, UpgradeBatteryComponent component, RefreshPartsEvent args)
        {
            var capacitorRating = args.PartRatings[component.MachinePartPowerCapacity];

            if (TryComp<BatteryComponent>(uid, out var batteryComp))
            {
                batteryComp.MaxCharge = MathF.Pow(component.MaxChargeMultiplier, capacitorRating - 1) * component.BaseMaxCharge;
            }
        }
    }
}
