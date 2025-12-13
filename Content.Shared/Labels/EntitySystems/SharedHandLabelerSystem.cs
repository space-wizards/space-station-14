using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
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
        SubscribeLocalEvent<HandLabelerComponent, ExaminedEvent>(OnExamined);
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

    private void AddLabelTo(Entity<HandLabelerComponent> ent, EntityUid user, EntityUid target)
    {
        if (ent.Comp.AssignedLabel == string.Empty)
        {
            RemoveLabelFrom(ent, user, target);
            return;
        }

        if (_netManager.IsServer)
            _labelSystem.Label(target, ent.Comp.AssignedLabel);

        _popupSystem.PopupClient(Loc.GetString("hand-labeler-successfully-applied"), user, user);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} labeled {ToPrettyString(target):target} with {ToPrettyString(ent):labeler}");
    }

    private void RemoveLabelFrom(EntityUid uid, EntityUid user, EntityUid target)
    {
        if (_netManager.IsServer)
            _labelSystem.Label(target, null);

        _popupSystem.PopupClient(Loc.GetString("hand-labeler-successfully-removed"), user, user);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} removed label from {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }

    private void OnUtilityVerb(Entity<HandLabelerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, target) || !args.CanAccess)
            return;

        var user = args.User;   // can't use ref parameter in lambdas

        if (ent.Comp.AssignedLabel != string.Empty)
        {
            var labelVerb = new UtilityVerb()
            {
                Act = () =>
                {
                    AddLabelTo(ent, user, target);
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
                RemoveLabelFrom(ent, user, target);
            },
            Text = Loc.GetString("hand-labeler-remove-label-text"),
            Priority = -1,
        };

        args.Verbs.Add(unLabelVerb);
    }

    private void AfterInteractOn(Entity<HandLabelerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target || _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, target) || !args.CanReach)
            return;

        AddLabelTo(ent, args.User, target);
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

    private void OnExamined(Entity<HandLabelerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = ent.Comp.AssignedLabel == string.Empty
            ? Loc.GetString("hand-labeler-examine-blank")
            : Loc.GetString("hand-labeler-examine-label-text", ("label-text", ent.Comp.AssignedLabel));
        args.PushMarkup(text);
    }
}
