using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Lock.BypassLock.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock.BypassLock.Systems;

public sealed partial class BypassLockSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BypassLockComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LockComponent, ForceOpenLockDoAfterEvent>(OnBypassAccessDoAfterEvent);
        SubscribeLocalEvent<BypassLockComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);

        InitializeMobStateLockSystem();
    }

    private void OnInteractUsing(Entity<BypassLockComponent> target, ref InteractUsingEvent args)
    {
        if (target.Owner == args.User)
            return;
        
        if (!_tool.HasQuality(args.Used, target.Comp.BypassingTool)
            || !_lock.IsLocked(target.Owner))
            return;

        var ev = new ForceOpenLockAttemptEvent(args.User);
        RaiseLocalEvent(target.Owner, ref ev);

        if (!ev.CanForceOpen)
            return;

        args.Handled = TryStartDoAfter(target, args.User, args.Used);
    }

    private bool TryStartDoAfter(Entity<BypassLockComponent> target, EntityUid user, EntityUid used)
    {
        if (!_tool.UseTool(
                used,
                user,
                target,
                (float) target.Comp.BypassDelay.TotalSeconds,
                target.Comp.BypassingTool,
                new ForceOpenLockDoAfterEvent()))
        {
            return false;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} is prying {ToPrettyString(target):target}'s lock open at {Transform(target).Coordinates:targetlocation}");
        return true;
    }

    private void OnBypassAccessDoAfterEvent(Entity<LockComponent> target, ref ForceOpenLockDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _lock.Unlock(target, args.User, target.Comp);
    }

    private void OnGetVerb(Entity<BypassLockComponent> target, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Using == null)
            return;

        var rightTool = _tool.HasQuality(args.Using.Value, target.Comp.BypassingTool);
        var item = args.Using.Value;
        var bypassVerb = new InteractionVerb
        {
            IconEntity = GetNetEntity(item),
        };

        bypassVerb.Text = bypassVerb.Message = Loc.GetString("bypass-lock-verb");

        var ev = new CheckBypassLockVerbRequirements(bypassVerb, rightTool, rightTool, target.Comp.BypassingTool);
        RaiseLocalEvent(target, ref ev);

        if (!ev.ShowVerb)
            return;

        var user = args.User;

        bypassVerb.Act = () => TryStartDoAfter(target, user, item);

        if (!_lock.IsLocked(target.Owner))
        {
            bypassVerb.Disabled = true;
            bypassVerb.Message = Loc.GetString("bypass-lock-disabled-already-open");
        }

        args.Verbs.Add(bypassVerb);
    }
}

/// <summary>
/// This event gets raised on the entity with the <see cref="BypassLockRequiresMobStateComponent"/> after someone finished
/// a doafter forcing the lock open.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ForceOpenLockDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// This gets raised on the target whose lock is attempted to be forced open.
/// </summary>
/// <param name="User">Entity attempting to open this.</param>
/// <param name="CanForceOpen">Whether the lock can be forced open.</param>
[ByRefEvent]
public record struct ForceOpenLockAttemptEvent(EntityUid User, bool CanForceOpen = true);

/// <summary>
/// This gets raised on the target that is being right-clicked to check for verb requirements.
/// </summary>
/// <param name="Verb">The interaction verb that will be shown.</param>
/// <param name="RightTool">Whether the tool has the right properties to force the lock open.</param>
/// <param name="ShowVerb">Whether the verb should be shown.</param>
/// <param name="ToolQuality">The required tool quality to force the lock open.</param>
[ByRefEvent]
public record struct CheckBypassLockVerbRequirements(InteractionVerb Verb, bool RightTool, bool ShowVerb, ProtoId<ToolQualityPrototype> ToolQuality);
