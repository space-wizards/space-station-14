using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Cancels solution transfers into a target container,
/// if at least one of the incoming reagents are not whitelisted.
/// </summary>
public sealed class RefillReagentWhitelistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillReagentWhitelistComponent, GetSolutionTransferWhitelistEvent>(OnGetWhitelist);
    }

    private void OnGetWhitelist(Entity<RefillReagentWhitelistComponent> ent, ref GetSolutionTransferWhitelistEvent args)
    {
        // Only contribute when this entity is the transfer target.
        if (ent.Owner != args.To)
            return;

        // Ensure the target actually has a relevant solution container.
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var _))
            return;

        // Only enforce for the specific named solution, provided by the event.
        if (!string.Equals(args.TargetSolutionName, ent.Comp.Solution, StringComparison.Ordinal))
            return;

        // Contribute this component's whitelist and mark enforcement.
        args.Enforce = true;
        if (ent.Comp.Popup is { } popup)
            args.Popup = popup;

        foreach (var allow in ent.Comp.Allowed)
            args.Allowed.Add(allow);
    }
}
