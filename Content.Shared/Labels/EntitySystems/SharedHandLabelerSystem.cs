namespace Content.Shared.Labels.EntitySystems;

public abstract class SharedHandLabelerSystem : EntitySystem {

    public override void Initialize(){
        base.Initialize();

        SubscribeLocalEvent<HandLabelerComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<HandLabelerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        // Bound UI subscriptions
        SubscribeLocalEvent<HandLabelerComponent, HandLabelerLabelChangedMessage>(OnHandLabelerLabelChanged);
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
            _labelSystem.Label(target, null);
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }

        _labelSystem.Label(target, handLabeler.AssignedLabel);
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    private void OnUtilityVerb(EntityUid uid, HandLabelerComponent handLabeler, GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || !handLabeler.Whitelist.IsValid(target) || !args.CanAccess)
            return;

        string labelerText = handLabeler.AssignedLabel == string.Empty ? Loc.GetString("hand-labeler-remove-label-text") : Loc.GetString("hand-labeler-add-label-text");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                AddLabelTo(uid, handLabeler, target, out var result);
                if (result != null)
                    _popupSystem.PopupEntity(result, args.User, args.User);
            },
            Text = labelerText
        };

        args.Verbs.Add(verb);
    }

    private void AfterInteractOn(EntityUid uid, HandLabelerComponent handLabeler, AfterInteractEvent args)
    {
        if (args.Target is not {Valid: true} target || !handLabeler.Whitelist.IsValid(target) || !args.CanReach)
            return;

        AddLabelTo(uid, handLabeler, target, out string? result);
        if (result == null)
            return;
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
        DirtyUI(uid, handLabeler);

        // Log label change
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(player):user} set {ToPrettyString(uid):labeler} to apply label \"{handLabeler.AssignedLabel}\"");

    }
}
