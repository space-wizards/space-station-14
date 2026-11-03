using Content.Shared.GameTicking;
using Content.Shared.Whitelist;

namespace Content.Server.GameTicking;

/// <inheritdoc/>
public sealed partial class ServerRuleGridsSystem : RuleGridsSystem
{
    [SubscribeLocalEvent]
    private void OnGridSplit(ref GridSplitEvent args)
    {
        var rule = QueryActiveRules();
        while (rule.MoveNext(out var comp, out _, out _))
        {
            if (!comp.MapGrids.Contains(args.Grid))
                continue;

            comp.MapGrids.AddRange(args.NewGrids);
            break; // only 1 rule can own a grid, not multiple
        }
    }
}
