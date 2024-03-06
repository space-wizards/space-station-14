using Content.Shared.Examine;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract partial class SharedBatterySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableBatteryComponent, ExaminedEvent>(OnExamine);
    }
    private void OnExamine(EntityUid uid, ExaminableBatteryComponent component, ExaminedEvent args)
    {
        if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
            return;
        if (args.IsInDetailsRange)
        {
            var effectiveMax = batteryComponent.MaxCharge;
            if (effectiveMax == 0)
                effectiveMax = 1;
            var chargeFraction = batteryComponent.CurrentCharge / effectiveMax;
            var chargePercentRounded = (int) (chargeFraction * 100);
            args.PushMarkup(
                Loc.GetString(
                    "examinable-battery-component-examine-detail",
                    ("percent", chargePercentRounded),
                    ("markupPercentColor", "green")
                )
            );
        }
    }
}
