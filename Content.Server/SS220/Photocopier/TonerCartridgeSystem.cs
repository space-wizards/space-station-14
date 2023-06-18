// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.SS220.Photocopier;

namespace Content.Server.SS220.Photocopier;

/// <summary>
/// This exists for an entire purpose of making toner cartridges state their fullness on examine.
/// </summary>
public sealed class TonerCartridgeSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TonerCartridgeComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, TonerCartridgeComponent component, ExaminedEvent args)
    {
        var fullness = component.Capacity == 0 ? 0 : (float)component.Charges / component.Capacity;
        var amountLocId = fullness switch
        {
            (>= 1) => "toner-cartridge-full",
            (>= 0.6f and < 1) => "toner-cartridge-nearly-full",
            (>= 0.4f and < 0.6f) => "toner-cartridge-half-full",
            (> 0 and < 0.4f) => "toner-cartridge-nearly-empty",
            (<= 0) => "toner-cartridge-empty",
            _ => "toner-cartridge-empty"
        };

        args.PushMarkup(Loc.GetString(amountLocId));
    }
}
