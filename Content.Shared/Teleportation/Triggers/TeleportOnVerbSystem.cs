using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Adds configurable teleport verbs and handles their activation.
/// </summary>
public sealed partial class TeleportOnVerbSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeVerb(TeleportVerbType.Verb, () => new Verb());
        SubscribeVerb(TeleportVerbType.Alternative, () => new AlternativeVerb());
        SubscribeVerb(TeleportVerbType.Interaction, () => new InteractionVerb());
        SubscribeVerb(TeleportVerbType.Activation, () => new ActivationVerb());
    }

    private void SubscribeVerb<TVerb>(TeleportVerbType type, Func<TVerb> createVerb) where TVerb : Verb
    {
        SubscribeLocalEvent<TeleportOnVerbComponent, GetVerbsEvent<TVerb>>(
            (Entity<TeleportOnVerbComponent> ent, ref GetVerbsEvent<TVerb> args) =>
                OnGetVerbs(ent, ref args, type, createVerb));
    }

    /// <summary>
    /// Builds the teleport menu entry, hiding or disabling it when unavailable and setting its explanatory message.
    /// </summary>
    private void OnGetVerbs<TVerb>(
        Entity<TeleportOnVerbComponent> ent,
        ref GetVerbsEvent<TVerb> args,
        TeleportVerbType type,
        Func<TVerb> createVerb)
        where TVerb : Verb
    {
        if (ent.Comp.VerbType != type)
            return;

        if (!args.CanAccess)
            return;

        if (ent.Comp.RequireCanInteract && !args.CanInteract)
            return;

        if (ent.Comp.RequireHands && args.Hands == null)
            return;

        if (!IsUserAllowed(ent.Comp, args.User))
            return;

        var target = args.User;
        var attempt = _teleport.CheckTeleportUse(ent, target, target);

        if (attempt.Cancelled && ent.Comp.HideWhenDisabled)
            return;

        var verb = createVerb();
        verb.Priority = ent.Comp.Priority;
        verb.Act = () => RequestTeleport(ent, target);
        verb.Disabled = attempt.Cancelled;
        verb.Text = Loc.GetString(ent.Comp.VerbText);
        verb.Message = GetMessage(ent.Comp, attempt);
        verb.Icon = ent.Comp.VerbIcon;
        var category = ent.Comp.VerbCategory;
        verb.Category = category != null ? new VerbCategory(category.Value, null) : null;

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Rechecks the verb's user filters, then requests teleportation with the configured effects.
    /// </summary>
    private void RequestTeleport(Entity<TeleportOnVerbComponent> ent, EntityUid target)
    {
        if (!IsUserAllowed(ent.Comp, target))
            return;

        _teleport.RequestTeleport(ent, target, target, ent.Comp.TriggerEffects);
    }

    /// <summary>
    /// Checks the verb's user whitelist and blacklist. Collision-trigger filters do not apply.
    /// </summary>
    private bool IsUserAllowed(TeleportOnVerbComponent component, EntityUid user)
    {
        return _whitelist.CheckBoth(
            user,
            blacklist: component.UserBlacklist,
            whitelist: component.UserWhitelist);
    }

    /// <summary>
    /// Returns the localized verb message. A cancellation reason overrides the default disabled message.
    /// </summary>
    private string? GetMessage(TeleportOnVerbComponent component, TeleportUseAttemptEvent attempt)
    {
        var message = attempt.Cancelled
            ? attempt.CancelReason ?? component.DisabledMessage
            : component.EnabledMessage;
        if (message == null)
            return null;

        return Loc.GetString(message.Value);
    }
}
