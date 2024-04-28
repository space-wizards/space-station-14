using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared.Labels.EntitySystems;

public sealed class HandLabelerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedLabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandLabelerComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
    }

    private void OnAfterState(Entity<HandLabelerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_userInterfaceSystem.TryGetOpenUi(ent.Owner, HandLabelerUiKey.Key, out var bui) ||
            bui is not HandLabel)
            return;

        bui.
    }

    private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            result = null;
            return;
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            if (_netManager.IsServer)
                _labelSystem.Label(target, null);
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }
        if (_netManager.IsServer)
            _labelSystem.Label(target, handLabeler.AssignedLabel);
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanAccess)
            return;

        var labelerText = handLabeler.AssignedLabel == string.Empty ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                Labeling(uid, target, args.User, handLabeler);
            },
            Text = labelerText
        };

        args.Verbs.Add(verb);
    }

    private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanReach)
            return;

        Labeling(uid, target, args.User, handLabeler);
    }

    private void Labeling(EntityUid uid, EntityUid target, EntityUid User, HandLabelerComponent handLabeler)
    {
        AddLabelTo(uid, handLabeler, target, out var result);
        if (result == null)
            return;

        _popupSystem.PopupClient(result, User, User);
        Dirty(uid, handLabeler);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(User):user} labeled {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }

    private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
    {
        var label = args.Label.Trim();
        handLabeler.AssignedLabel = label[..Math.Min(handLabeler.MaxLabelChars, label.Length)];
        Dirty(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");
    }
}
