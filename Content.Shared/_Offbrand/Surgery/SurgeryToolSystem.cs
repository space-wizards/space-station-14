using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._Offbrand.Surgery;

// this code needs to use predicted popups when construction gets predicted
public sealed class SurgeryToolSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, ToolUseAttemptEvent>(OnToolAttemptUse);
    }

    private void OnToolAttemptUse(Entity<SurgeryToolComponent> ent, ref ToolUseAttemptEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (_inventory.TryGetContainerSlotEnumerator(target, out var enumerator, ent.Comp.SlotsToCheck))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is not { } contained)
                    continue;

                if (_entityWhitelist.CheckBoth(contained, ent.Comp.Blacklist, ent.Comp.Whitelist))
                    continue;

                _popup.PopupCursor(Loc.GetString(ent.Comp.SlotsDenialPopup, ("target", args.Target), ("clothing", contained)), args.User);
                args.Cancel();
                return;
            }
        }


        if (!_standingState.IsDown(target))
        {
            _popup.PopupCursor(Loc.GetString(ent.Comp.DownDenialPopup, ("target", args.Target)), args.User);
            args.Cancel();

            return;
        }
    }
}
