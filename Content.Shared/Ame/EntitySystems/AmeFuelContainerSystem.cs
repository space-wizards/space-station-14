using Content.Shared.Ame.Components;
using Content.Shared.Examine;

namespace Content.Shared.Ame.EntitySystems;

/// <summary>
/// Adds details about fuel level when examining antimatter engine fuel containers.
/// </summary>
public sealed class AmeFuelContainerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeFuelContainerComponent, ExaminedEvent>(OnFuelExamined);
    }

    private void OnFuelExamined(EntityUid uid, AmeFuelContainerComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // less than 25%: amount < capacity / 4 = amount * 4 < capacity
        var low = comp.FuelAmount * 4 < comp.FuelCapacity;
        args.PushMarkup(Loc.GetString("ame-fuel-container-component-on-examine-detailed-message",
            ("colorName", low ? "darkorange" : "orange"),
            ("amount", comp.FuelAmount),
            ("capacity", comp.FuelCapacity)));
    }
}
