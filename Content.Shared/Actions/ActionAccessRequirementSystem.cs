using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;

namespace Content.Shared.Actions;

public sealed class ActionAccessRequirementSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionAccessRequirementComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(EntityUid uid, ActionAccessRequirementComponent component, ref ActionAttemptEvent args)
    {
        var (allowed, reason) = IsAllowed(component, args.User);
        if (allowed)
            return;

        args.Cancelled = true;

        switch (reason)
        {
            case AccessReason.Whitelist:
                _popup.PopupClient(Loc.GetString("action-access-requirement-missing"), args.User, args.User);
                break;
            default:
                _popup.PopupClient(Loc.GetString("action-access-requirement-denied"), args.User, args.User);
                break;
        }
    }

    public (bool, AccessReason?) IsAllowed(ActionAccessRequirementComponent component, EntityUid user)
    {
        var accessTags = _accessReader.FindAccessTags(user).Select(p => p.Id).ToHashSet();
        return IsAllowed(component, accessTags);
    }

    public (bool, AccessReason?) IsAllowed(ActionAccessRequirementComponent component, ICollection<string> accessTags)
    {
        if (component.Blacklist != null && accessTags.Any(tag => component.Blacklist.Contains(tag)))
        {
            return (false, AccessReason.Blacklist);
        }

        if (component.Whitelist != null && !accessTags.Any(tag => component.Whitelist.Contains(tag)))
        {
            return (false, AccessReason.Whitelist);
        }

        return (true, null);
    }

    public enum AccessReason : byte
    {
        Whitelist,
        Blacklist,
    }
}
