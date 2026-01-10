using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
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

        SubscribeLocalEvent<Components.ActionAccessRequirementComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(EntityUid uid, Components.ActionAccessRequirementComponent component, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var accessTags = _accessReader.FindAccessTags(args.User);

        if (component.Blacklist != null && accessTags.Any(tag => component.Blacklist.Contains(tag)))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("action-access-requirement-denied"), args.User, args.User);
            return;
        }

        if (component.Whitelist != null && !accessTags.Any(tag => component.Whitelist.Contains(tag)))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("action-access-requirement-missing"), args.User, args.User);
        }
    }
}
