using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.Actions;

public sealed partial class ActionRestrictionsSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnWhitelistAttempt(Entity<ActionUserWhitelistComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled || _whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.User))
            return;

        CancelAttempt(args.User, ent.Comp.OnFailPopup, ent.Comp.OnFailPopupType, ref args);
    }

    [SubscribeLocalEvent]
    private void OnProviderHeldAttempt(Entity<ActionProviderHeldComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var provider = Comp<ActionComponent>(ent).Container;
        if (_hands.IsHolding(args.User, provider))
            return;

        CancelAttempt(args.User, ent.Comp.OnFailPopup, ent.Comp.OnFailPopupType, ref args);
    }

    private void CancelAttempt(EntityUid user, LocId? popup, PopupType type, ref ActionAttemptEvent args)
    {
        if (popup != null)
            _popup.PopupEntity(Loc.GetString(popup.Value), user, user,  type);

        args.Cancelled = true;
    }
}
