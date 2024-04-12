using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Labels.EntitySystems;

public abstract class SharedHandLabelerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedLabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandLabelerComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<HandLabelerComponent, ComponentGetState>(GetCompState);

        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
    }

    protected virtual void DirtyUI(EntityUid uid, HandLabelerComponent? handLabeler = null) { }

    private void GetCompState(Entity<HandLabelerComponent> ent, ref ComponentGetState args)
    {
        args.State = new HandLabelerComponentState
        {
            AssignedLabel = ent.Comp.AssignedLabel,
            MaxLabelChars = ent.Comp.MaxLabelChars,
            RecentlyLabeled = ent.Comp.RecentlyLabeled
        };

        ent.Comp.RecentlyLabeled.Clear();
    }

    private void HandleCompState(Entity<HandLabelerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not HandLabelerComponentState state)
            return;

        ent.Comp.AssignedLabel = state.AssignedLabel;
        ent.Comp.MaxLabelChars = state.MaxLabelChars;

        foreach (var item in state.RecentlyLabeled)
        {
            if (!ent.Comp.RecentlyLabeled.TryGetValue(item.Key, out var predicted))
                return;

            if (predicted == item.Value)
                return;

            string result = item.Value == LabelAction.Removed ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");
            _popupSystem.PopupClient(result, ent, null);
        }
    }

    protected virtual void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
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
            handLabeler.RecentlyLabeled.TryAdd(GetNetEntity(target), LabelAction.Removed);
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }
        if (_netManager.IsServer)
            _labelSystem.Label(target, handLabeler.AssignedLabel);
        handLabeler.RecentlyLabeled.TryAdd(GetNetEntity(target), LabelAction.Applied);
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if (_netManager.IsClient && !_timing.IsFirstTimePredicted)
            return;
        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanAccess)
            return;

        string labelerText = handLabeler.AssignedLabel == string.Empty ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");

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
        if (_netManager.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanReach)
            return;

        Labeling(uid, target, args.User, handLabeler);
    }

    private void Labeling(EntityUid uid, EntityUid target, EntityUid User, HandLabelerComponent handLabeler)
    {
        AddLabelTo(uid, handLabeler, target, out string? result);
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
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        var label = args.Label.Trim();
        handLabeler.AssignedLabel = label.Substring(0, Math.Min(handLabeler.MaxLabelChars, label.Length));
        if (_netManager.IsServer)
            DirtyUI(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(player):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");
    }
}
