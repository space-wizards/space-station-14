using Content.Server.AME.Components;
using Content.Shared.Examine;

namespace Content.Server.AME;

/// <summary>
/// Adds fuel level info to examine on fuel jars and handles network state.
/// </summary>
public sealed partial class AMESystem
{
    private void InitializeFuel()
    {
        SubscribeLocalEvent<AMEFuelContainerComponent, ExaminedEvent>(OnFuelExamined);
    }

    private void OnFuelExamined(EntityUid uid, AMEFuelContainerComponent comp, ExaminedEvent args)
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
