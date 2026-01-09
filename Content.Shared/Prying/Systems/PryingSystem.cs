using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using PryUnpoweredComponent = Content.Shared.Prying.Components.PryUnpoweredComponent;

namespace Content.Shared.Prying.Systems;

/// <summary>
/// Handles prying of entities (e.g. doors)
/// </summary>
public sealed class PryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Mob prying doors
        SubscribeLocalEvent<PryableComponent, GetVerbsEvent<AlternativeVerb>>(OnPryableAltVerb);
        SubscribeLocalEvent<PryableComponent, PryDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PryableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, PryableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryPry(uid, args.User, out _, args.Used);
    }

    private void OnPryableAltVerb(Entity<PryableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<PryingComponent>(args.User, out var prying))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.VerbLocStr),
            Impact = LogImpact.Low,
            Act = () => TryPry(ent, user, out _, user, ent.Comp, prying),
        });
    }

    /// <summary>
    /// Attempt to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id, EntityUid tool, PryableComponent? pryable = null, PryingComponent? prying = null)
    {
        id = null;

        if (!Resolve(target, ref pryable, logMissing: false))
            return false;

        if (!Resolve(tool, ref prying, logMissing: false))
            return false;

        if (!prying.Enabled)
            return false;

        if (!CanPry(target, tool, out var message, prying, pryable))
        {
            if (!string.IsNullOrWhiteSpace(message))
                _popup.PopupClient(message, target, user);
            // If we have reached this point we want the event that caused this
            // to be marked as handled.
            return true;
        }

        StartPry((target, pryable), user, (tool, prying), prying.SpeedModifier, out id);

        return true;
    }

    /// <summary>
    /// Try to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id, PryUnpoweredComponent? unpoweredComp = null, PryableComponent? pryable = null)
    {
        id = null;

        if (!Resolve(target, ref pryable, logMissing: false))
            return false;

        // We don't care about displaying a message if no tool was used.
        if (!Resolve(target, ref unpoweredComp, logMissing: false)
            || !CanPry(target, user, out _, pryable: pryable, unpoweredComp: unpoweredComp))
            // If we have reached this point we want the event that caused this
            // to be marked as handled.
            return true;

        // hand-prying is much slower
        var modifier = CompOrNull<PryingComponent>(user)?.SpeedModifier ?? unpoweredComp.PryModifier;
        return StartPry((target, pryable), user, null, modifier, out id);
    }

    private bool CanPry(EntityUid target, EntityUid user, out string? message, PryingComponent? prying = null, PryableComponent? pryable = null, PryUnpoweredComponent? unpoweredComp = null)
    {
        BeforePryEvent canev;

        message = null;

        if (!Resolve(target, ref pryable, logMissing: false))
            return false;

        // Are we prying with a tool?
        if (Resolve(user, ref prying, false))
        {
            // Check if we can pry this entity with this tool
            canev = new BeforePryEvent(user, prying.Strength);
        }
        else
        {
            // Can this entity be pried without tools?
            if (!Resolve(target, ref unpoweredComp))
                return false;

            // Check if we can pry this entity without tools in its current state
            canev = new BeforePryEvent(user, PryStrength.Weak);
        }

        RaiseLocalEvent(target, ref canev);

        message = canev.Message;

        return canev.CanPry;
    }

    private bool StartPry(Entity<PryableComponent> target, EntityUid user, Entity<PryingComponent>? tool, float toolModifier, [NotNullWhen(true)] out DoAfterId? id)
    {
        var modEv = new GetPryTimeModifierEvent(user, target.Comp.PryTime);

        var toolIsUser = tool?.Owner == user;

        RaiseLocalEvent(target, ref modEv);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, modEv.BaseTime * modEv.PryTimeModifier / toolModifier, new PryDoAfterEvent(), target, target, tool?.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = !toolIsUser,
        };

        if (!toolIsUser && tool != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool.Value)} to pry {ToPrettyString(target)}");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is prying {ToPrettyString(target)}");
        }
        return _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
    }

    private void OnDoAfter(Entity<PryableComponent> ent, ref PryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        var pryUser = args.User;
        if (TryComp<PryingComponent>(args.Used, out var prying))
            pryUser = args.Used.Value;

        if (!CanPry(ent, pryUser, out var message, prying: prying, pryable: ent.Comp))
        {
            if (!string.IsNullOrWhiteSpace(message))
                _popup.PopupClient(message, ent, args.User);
            return;
        }

        if (args.Used != null && prying != null)
        {
            _audioSystem.PlayPredicted(prying.UseSound, args.Used.Value, args.User);
        }

        var ev = new PriedEvent(args.User);
        RaiseLocalEvent(ent, ref ev);
    }

    public static void SetPryingEnabled(Entity<PryingComponent> ent, bool value)
    {
        ent.Comp.Enabled = value;
    }

    public static void SetPryingSpeedModifier(Entity<PryingComponent> ent, float value)
    {
        ent.Comp.SpeedModifier = value;
    }

    public static void SetPryingStrength(Entity<PryingComponent> ent, PryStrength value)
    {
        ent.Comp.Strength = value;
    }
}

[Serializable, NetSerializable]
public sealed partial class PryDoAfterEvent : SimpleDoAfterEvent;
