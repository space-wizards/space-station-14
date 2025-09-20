using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.Labels.EntitySystems;

public abstract class SharedHandLabelerSystem : EntitySystem
{
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
        SubscribeLocalEvent<HandLabelerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<HandLabelerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(Entity<HandLabelerComponent> ent, ref ComponentGetState args)
    {
        args.State = new HandLabelerComponentState(ent.Comp.AssignedLabel)
        {
            MaxLabelChars = ent.Comp.MaxLabelChars,
        };
    }

    private void OnHandleState(Entity<HandLabelerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not HandLabelerComponentState state)
            return;

        ent.Comp.MaxLabelChars = state.MaxLabelChars;

        if (ent.Comp.AssignedLabel == state.AssignedLabel)
            return;

        ent.Comp.AssignedLabel = state.AssignedLabel;
        UpdateUI(ent);
    }

    protected virtual void UpdateUI(Entity<HandLabelerComponent> ent)
    {
    }

    private void AddLabelTo(EntityUid uid, EntityUid User, HandLabelerComponent? handLabeler, EntityUid target)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            return;
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            RemoveLabelFrom(uid, User, handLabeler, target);
            return;
        }

        if (_netManager.IsServer)
            _labelSystem.Label(target, handLabeler.AssignedLabel);

        _popupSystem.PopupClient(Loc.GetString("hand-labeler-successfully-applied"), User, User);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(User):user} labeled {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }

    private void RemoveLabelFrom(EntityUid uid, EntityUid User, HandLabelerComponent? handLabeler, EntityUid target)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            return;
        }

        if (_netManager.IsServer)
            _labelSystem.Label(target, null);

        _popupSystem.PopupClient(Loc.GetString("hand-labeler-successfully-removed"), User, User);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(User):user} removed label from {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(handLabeler.Whitelist, target) || !args.CanAccess)
            return;

        bool labelerBlank = (handLabeler.AssignedLabel == string.Empty);

        if (!labelerBlank)
        {
            var labelVerb = new UtilityVerb()
            {
                Act = () =>
                {
                    AddLabelTo(uid, args.User, handLabeler, target);
                },
                Text = Loc.GetString("hand-labeler-add-label-text")
            };

            args.Verbs.Add(labelVerb);
        }

        // add the unlabel verb to the menu even when the labeler has text
        var unLabelVerb = new UtilityVerb()
        {
            Act = () =>
            {
                RemoveLabelFrom(uid, args.User, handLabeler, target);
            },
            Text = Loc.GetString("hand-labeler-remove-label-text"),
            Priority = -1,
        };

        if (!labelerBlank)
        {
            unLabelVerb.TextStyleClass = Verb.DefaultTextStyleClass;
        }

        args.Verbs.Add(unLabelVerb);
    }

    private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(handLabeler.Whitelist, target) || !args.CanReach)
            return;

        AddLabelTo(uid, args.User, handLabeler, target);
    }

    private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
    {
        var label = args.Label.Trim();
        handLabeler.AssignedLabel = label[..Math.Min(handLabeler.MaxLabelChars, label.Length)];
        UpdateUI((uid, handLabeler));
        Dirty(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");
    }
}
