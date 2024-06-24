using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
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
        SubscribeLocalEvent<DoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorAltVerb);
        SubscribeLocalEvent<DoorComponent, DoorPryDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DoorComponent, InteractUsingEvent>(TryPryDoor);
    }

    private void TryPryDoor(EntityUid uid, DoorComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryPry(uid, args.User, out _, args.Used);
    }

    private void OnDoorAltVerb(EntityUid uid, DoorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<PryingComponent>(args.User, out _))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("door-pry"),
            Impact = LogImpact.Low,
            Act = () => TryPry(uid, args.User, out _, args.User),
        });
    }

    /// <summary>
    /// Attempt to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id, EntityUid tool)
    {
        id = null;

        PryingComponent? comp = null;
        if (!Resolve(tool, ref comp, false))
            return false;

        if (!comp.Enabled)
            return false;

        if (!CanPry(target, user, out var message, comp))
        {
            if (!string.IsNullOrWhiteSpace(message))
                _popup.PopupClient(Loc.GetString(message), target, user);
            // If we have reached this point we want the event that caused this
            // to be marked as handled.
            return true;
        }

        StartPry(target, user, tool, comp.SpeedModifier, out id);

        return true;
    }

    /// <summary>
    /// Try to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id)
    {
        id = null;

        // We don't care about displaying a message if no tool was used.
        if (!TryComp<PryUnpoweredComponent>(target, out var unpoweredComp) || !CanPry(target, user, out _, unpoweredComp: unpoweredComp))
            // If we have reached this point we want the event that caused this
            // to be marked as handled.
            return true;

        // hand-prying is much slower
        var modifier = CompOrNull<PryingComponent>(user)?.SpeedModifier ?? unpoweredComp.PryModifier;
        return StartPry(target, user, null, modifier, out id);
    }

    private bool CanPry(EntityUid target, EntityUid user, out string? message, PryingComponent? comp = null, PryUnpoweredComponent? unpoweredComp = null)
    {
        BeforePryEvent canev;

        if (comp != null || Resolve(user, ref comp, false))
        {
            canev = new BeforePryEvent(user, comp.PryPowered, comp.Force, true);
        }
        else
        {
            if (!Resolve(target, ref unpoweredComp))
            {
                message = null;
                return false;
            }

            canev = new BeforePryEvent(user, false, false, false);
        }

        RaiseLocalEvent(target, ref canev);

        message = canev.Message;

        return !canev.Cancelled;
    }

    private bool StartPry(EntityUid target, EntityUid user, EntityUid? tool, float toolModifier, [NotNullWhen(true)] out DoAfterId? id)
    {
        var modEv = new GetPryTimeModifierEvent(user);

        RaiseLocalEvent(target, ref modEv);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(modEv.BaseTime * modEv.PryTimeModifier / toolModifier), new DoorPryDoAfterEvent(), target, target, tool)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = tool != user,
        };

        if (tool != user && tool != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool.Value)} to pry {ToPrettyString(target)}");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is prying {ToPrettyString(target)}");
        }
        return _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
    }

    private void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        TryComp<PryingComponent>(args.Used, out var comp);

        if (!CanPry(uid, args.User, out var message, comp))
        {
            if (!string.IsNullOrWhiteSpace(message))
                _popup.PopupClient(Loc.GetString(message), uid, args.User);
            return;
        }

        if (args.Used != null && comp != null)
        {
            _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User);
        }

        var ev = new PriedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);
    }
}

[Serializable, NetSerializable]
public sealed partial class DoorPryDoAfterEvent : SimpleDoAfterEvent;
