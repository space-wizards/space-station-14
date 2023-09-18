using Content.Shared.Prying.Components;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;

namespace Content.Shared.Prying.Systems;

public sealed class PryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

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

        args.Handled = TryPry(uid, args.User, comp, out _, args.Used);
    }

    private void OnDoorAltVerb(EntityUid uid, DoorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<PryingComponent>(args.User, out var tool))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("door-pry"),
            Impact = LogImpact.Low,
            Act = () => TryPry(uid, args.User, component, out _, args.User),
        });
    }

    /// <summary>
    /// Attempt to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, DoorComponent door, out DoAfterId? id, EntityUid tool)
    {
        id = null;

        PryingComponent? comp = null;
        if (!Resolve(tool, ref comp, false))
            return false;

        if (comp.NeedsComponent)
        {
            ToolComponent? tComp = null;
            if (!Resolve(tool, ref tComp) || !tComp.Qualities.Contains("Prying"))
                return false;
        }

        if (!CanPry(target, user, comp))
        {
            // If we have reached this point we want the event that caused this
            // to be marked as handled as a popup would be generated on failure.
            return true;
        }

        StartPry(target, user, door, tool, comp.SpeedModifier, out id);

        return true;
    }

    /// <summary>
    /// Try to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, DoorComponent door, out DoAfterId? id)
    {
        id = null;

        if (!CanPry(target, user))
            // If we have reached this point we want the event that caused this
            // to be marked as handled as a popup would be generated on failure.
            return true;

        return StartPry(target, user, door, null, 1.0f, out id);
    }

    private bool CanPry(EntityUid target, EntityUid user, PryingComponent? comp = null)
    {
        BeforePryEvent canev;

        if (comp != null)
        {
            canev = new BeforePryEvent(user, comp.PryPowered);
        }
        else
        {
            if (!TryComp<PryUnpoweredComponent>(target, out _))
                return false;
            canev = new BeforePryEvent(user, false);
        }

        RaiseLocalEvent(target, canev, false);

        if (canev.Cancelled)
            return false;
        return true;
    }

    private bool StartPry(EntityUid target, EntityUid user, DoorComponent door, EntityUid? tool, float toolModifier, [NotNullWhen(true)] out DoAfterId? id)
    {
        var modEv = new DoorGetPryTimeModifierEvent(user);


        RaiseLocalEvent(target, modEv, false);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(door.PryTime * modEv.PryTimeModifier / toolModifier), new DoorPryDoAfterEvent(), target, target, tool)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
        };

        if (tool != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool.Value)} to pry {ToPrettyString(target)} while it is {door.State}");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is prying {ToPrettyString(target)} while it is {door.State}");
        }
        return _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
    }

    private void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;


        PryingComponent? comp = null;

        if (args.Used != null && Resolve(args.Used.Value, ref comp))
            _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User);

        var ev = new AfterPryEvent(args.User);
        RaiseLocalEvent(uid, ev, false);

    }
}

[Serializable, NetSerializable]
public sealed partial class DoorPryDoAfterEvent : SimpleDoAfterEvent
{
}
