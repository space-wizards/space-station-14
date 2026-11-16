using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BreakerFlipRule))]
public sealed partial class BreakerFlipRuleComponent : Component
{
    /// <summary>
    /// The range of APCs affected by this game rule.
    /// </summary>
    [DataField]
    public MinMax AffectedQuantity = new MinMax(3, 6);
}
