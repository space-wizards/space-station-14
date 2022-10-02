using Content.Server.Construction;
using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.Power.SMES
{
    [UsedImplicitly]
    public sealed class MachineBatterySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MachineBatteryComponent, RefreshPartsEvent>(OnRefreshParts);
        }

        public void OnRefreshParts(EntityUid uid, MachineBatteryComponent component, RefreshPartsEvent args)
        {
            var capacitorRating = args.PartRatings[component.MachinePartPowerCapacity];

            if (TryComp<BatteryComponent>(uid, out var batteryComp))
            {
                batteryComp.MaxCharge = MathF.Pow(capacitorRating,component.MachinePartEfficiency) * component.PowerCapacityScaling;
            }
        }
    }
}
