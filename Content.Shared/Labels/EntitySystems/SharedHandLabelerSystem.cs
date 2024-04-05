using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
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

        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
    }

    protected virtual void messageClient(EntityUid targetItem, EntityUid user, LabelAction action){}
    protected virtual void DirtyUI(EntityUid uid, HandLabelerComponent? handLabeler = null){}

    protected virtual void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result, out LabelAction? action)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            result = null;
            action = null;
            return;
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            if(_netManager.IsServer)
                _labelSystem.Label(target, null);
            action = LabelAction.Removed;
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }
        if(_netManager.IsServer)
            _labelSystem.Label(target, handLabeler.AssignedLabel);
        action = LabelAction.Applied;
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if(_netManager.IsClient && !_timing.IsFirstTimePredicted)
            return;
        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanAccess)
            return;

        string labelerText = handLabeler.AssignedLabel == string.Empty ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                AddLabelTo(uid, handLabeler, target, out var result, out var action);
                if (result == null || action == null)
                    return;

                if(_netManager.IsServer)
                    messageClient(target,args.User, (LabelAction) action);
                else
                    _popupSystem.PopupEntity(result, args.User, args.User);
            },
            Text = labelerText
        };

        args.Verbs.Add(verb);
    }

    private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
    {
        if(_netManager.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (args.Target is not {Valid: true} target || !handLabeler.Whitelist.IsValid(target) || !args.CanReach)
            return;

        AddLabelTo(uid, handLabeler, target, out string? result, out LabelAction? action);
        if (result == null || action == null)
            return;

        if(_netManager.IsServer)
            messageClient(target,args.User, (LabelAction) action);
        else
            _popupSystem.PopupEntity(result, args.User, args.User);

        // Log labeling
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} labeled {ToPrettyString(target):target} with {ToPrettyString(uid):labeler}");
    }


    private void OnHandLabelerLabelChanged(EntityUid uid, HandLabelerComponent handLabeler, HandLabelerLabelChangedMessage args)
    {
        if (args.Session.AttachedEntity is not {Valid: true} player)
            return;

        var label = args.Label.Trim();
        handLabeler.AssignedLabel = label.Substring(0, Math.Min(handLabeler.MaxLabelChars, label.Length));
        if(_netManager.IsServer)
            DirtyUI(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(player):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");

    }
}

/// <summary>
/// Message the Client uses to detect mispredictions.
/// </summary>
/// <remarks>
/// Everytime a item is labeled, this message is sent to the client.
/// If the contents of this message differ from the clients prediction,
/// the client will inform the user of a misprediction.
/// </remarks>
/// <param name="Target">The NetEntity object of the item being labelled.</param>
/// <param name="User">The NetEntity of the user doing the labelling</param>
/// <param name="Action">Tells what the labelling action is doing.</param>
[Serializable, NetSerializable]
public sealed class HandLabelerMessage : EntityEventArgs
{
    public NetEntity Target { get; }
    public NetEntity User { get; }
    public LabelAction Action { get; }

    public HandLabelerMessage(NetEntity target, NetEntity user, LabelAction action)
    {
        Target = target;
        User = user;
        Action = action;
    }
}

/// <summary>
/// Different actions the HandLabeler can do.
/// </summary>
/// <remarks>
/// `invalid` value should never appear anywhere.
/// <see cref="HandLabelerMessage">
/// </remarks>
public enum LabelAction
{
    invalid,
    Removed,
    Applied
}
