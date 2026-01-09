using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock;

public sealed class MobStateBypassLockSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateBypassLockComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LockComponent, BypassLockDoAfterEvent>(OnBypassAccessDoAfterEvent);
        SubscribeLocalEvent<MobStateBypassLockComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnInteractUsing(Entity<MobStateBypassLockComponent> target, ref InteractUsingEvent args)
    {
        if (!_tool.HasQuality(args.Used, target.Comp.BypassingTool)
            || !TryComp<MobStateComponent>(target, out var mobState)
            || mobState.CurrentState < target.Comp.RequiredMobState
            || !_lock.IsLocked(target.Owner))
            return;

        if (TryStartDoAfter(target, args.User, args.Used))
            return;

        args.Handled = true;
    }

    private bool TryStartDoAfter(Entity<MobStateBypassLockComponent> target, EntityUid user, EntityUid used)
    {
        if (!_tool.UseTool(
                used,
                user,
                target,
                (float) target.Comp.BypassDelay.TotalSeconds,
                target.Comp.BypassingTool,
                new BypassLockDoAfterEvent()))
        {
            return true;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} is prying {ToPrettyString(target):target}'s lock open at {Transform(target).Coordinates:targetlocation}");
        return false;
    }

    private void OnBypassAccessDoAfterEvent(Entity<LockComponent> target, ref BypassLockDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _lock.Unlock(target, args.User, target.Comp);
    }

    private void OnGetVerb(Entity<MobStateBypassLockComponent> target, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Using == null
            || !TryComp<MobStateComponent>(target, out var mobState))
            return;

        var rightTool = _tool.HasQuality(args.Using.Value, target.Comp.BypassingTool);

        // Always show the tooltip on critical borgs, but show it only on alive ones with the right tool.
        if (!rightTool && target.Comp.RequiredMobState > mobState.CurrentState)
            return;

        var user = args.User;
        var item = args.Using.Value;

        var toggleVerb = new InteractionVerb
        {
            IconEntity = GetNetEntity(item),
        };

        toggleVerb.Text = toggleVerb.Message = Loc.GetString("bypass-lock-verb");

        if (target.Comp.RequiredMobState > mobState.CurrentState && rightTool)
        {
            toggleVerb.Disabled = true;
            toggleVerb.Message = Loc.GetString("bypass-lock-disabled-healthy");
        }
        else if (!_lock.IsLocked(target.Owner))
        {
            toggleVerb.Disabled = true;
            toggleVerb.Message = Loc.GetString("bypass-lock-disabled-already-open");
        }
        else if (!rightTool)
        {
            toggleVerb.Disabled = true;
            toggleVerb.Message = Loc.GetString("bypass-lock-disabled-wrong-tool", ("quality", target.Comp.BypassingTool.ToString().ToLower()));
        }

        toggleVerb.Act = () => TryStartDoAfter(target, user, item);

        args.Verbs.Add(toggleVerb);
    }
}

/// <summary>
/// This event gets raised on the entity with the <see cref="MobStateBypassLockComponent"/> after someone finished
/// a doafter prying open the lock.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BypassLockDoAfterEvent : SimpleDoAfterEvent;
