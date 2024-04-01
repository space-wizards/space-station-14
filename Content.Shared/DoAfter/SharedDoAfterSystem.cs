using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <summary>
    ///     We'll use an excess time so stuff like finishing effects can show.
    /// </summary>
    private static readonly TimeSpan ExcessTime = TimeSpan.FromSeconds(0.5f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoAfterComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<DoAfterComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<DoAfterComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<DoAfterComponent, ComponentGetState>(OnDoAfterGetState);
        SubscribeLocalEvent<DoAfterComponent, ComponentHandleState>(OnDoAfterHandleState);
    }

    private void OnUnpaused(EntityUid uid, DoAfterComponent component, ref EntityUnpausedEvent args)
    {
        foreach (var doAfter in component.DoAfters.Values)
        {
            doAfter.StartTime += args.PausedTime;
            if (doAfter.CancelledTime != null)
                doAfter.CancelledTime = doAfter.CancelledTime.Value + args.PausedTime;
        }

        Dirty(uid, component);
    }

    private void OnStateChanged(EntityUid uid, DoAfterComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.NewMobState != MobState.Critical)
            return;

        foreach (var doAfter in component.DoAfters.Values)
        {
            InternalCancel(doAfter, component);
        }
        Dirty(uid, component);
    }

    /// <summary>
    /// Cancels DoAfter if it breaks on damage and it meets the threshold
    /// </summary>
    private void OnDamage(EntityUid uid, DoAfterComponent component, DamageChangedEvent args)
    {
        // If we're applying state then let the server state handle the do_after prediction.
        // This is to avoid scenarios where a do_after is erroneously cancelled on the final tick.
        if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null || GameTiming.ApplyingState)
            return;

        var delta = args.DamageDelta.GetTotal();

        var dirty = false;
        foreach (var doAfter in component.DoAfters.Values)
        {
            if (doAfter.Args.BreakOnDamage && delta >= doAfter.Args.DamageThreshold)
            {
                InternalCancel(doAfter, component);
                dirty = true;
            }
        }

        if (dirty)
            Dirty(uid, component);
    }

    private void RaiseDoAfterEvents(DoAfter doAfter, DoAfterComponent component)
    {
        var ev = doAfter.Args.Event;
        ev.Handled = false;
        ev.Repeat = false;
        ev.DoAfter = doAfter;

        if (Exists(doAfter.Args.EventTarget))
            RaiseLocalEvent(doAfter.Args.EventTarget.Value, (object)ev, doAfter.Args.Broadcast);
        else if (doAfter.Args.Broadcast)
            RaiseLocalEvent((object)ev);

        if (component.AwaitedDoAfters.Remove(doAfter.Index, out var tcs))
            tcs.SetResult(doAfter.Cancelled ? DoAfterStatus.Cancelled : DoAfterStatus.Finished);
    }

    private void OnDoAfterGetState(EntityUid uid, DoAfterComponent comp, ref ComponentGetState args)
    {
        args.State = new DoAfterComponentState(EntityManager, comp);
    }

    private void OnDoAfterHandleState(EntityUid uid, DoAfterComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not DoAfterComponentState state)
            return;

        // Note that the client may have correctly predicted the creation of a do-after, but that doesn't guarantee that
        // the contents of the do-after data are correct. So this just takes the brute force approach and completely
        // overwrites the state.

        comp.DoAfters.Clear();
        foreach (var (id, doAfter) in state.DoAfters)
        {
            var newDoAfter = new DoAfter(EntityManager, doAfter);
            comp.DoAfters.Add(id, newDoAfter);

            // Networking yay (if you have an easier way dear god please).
            newDoAfter.UserPosition = EnsureCoordinates<DoAfterComponent>(newDoAfter.NetUserPosition, uid);
            newDoAfter.InitialItem = EnsureEntity<DoAfterComponent>(newDoAfter.NetInitialItem, uid);

            var doAfterArgs = newDoAfter.Args;
            doAfterArgs.Target = EnsureEntity<DoAfterComponent>(doAfterArgs.NetTarget, uid);
            doAfterArgs.Used = EnsureEntity<DoAfterComponent>(doAfterArgs.NetUsed, uid);
            doAfterArgs.User = EnsureEntity<DoAfterComponent>(doAfterArgs.NetUser, uid);
            doAfterArgs.EventTarget = EnsureEntity<DoAfterComponent>(doAfterArgs.NetEventTarget, uid);
        }

        comp.NextId = state.NextId;
        DebugTools.Assert(!comp.DoAfters.ContainsKey(comp.NextId));

        if (comp.DoAfters.Count == 0)
            RemCompDeferred<ActiveDoAfterComponent>(uid);
        else
            EnsureComp<ActiveDoAfterComponent>(uid);
    }

    #region Creation
    /// <summary>
    ///     Tasks that are delayed until the specified time has passed
    ///     These can be potentially cancelled by the user moving or when other things happen.
    /// </summary>
    // TODO remove this, as well as AwaitedDoAfterEvent and DoAfterComponent.AwaitedDoAfters
    [Obsolete("Use the synchronous version instead.")]
    public async Task<DoAfterStatus> WaitDoAfter(DoAfterArgs doAfter, DoAfterComponent? component = null)
    {
        if (!Resolve(doAfter.User, ref component))
            return DoAfterStatus.Cancelled;

        if (!TryStartDoAfter(doAfter, out var id, component))
            return DoAfterStatus.Cancelled;

        if (doAfter.Delay <= TimeSpan.Zero)
        {
            Log.Warning("Awaited instant DoAfters are not supported fully supported");
            return DoAfterStatus.Finished;
        }

        var tcs = new TaskCompletionSource<DoAfterStatus>();
        component.AwaitedDoAfters.Add(id.Value.Index, tcs);
        return await tcs.Task;
    }

    /// <summary>
    ///     Attempts to start a new DoAfter. Note that even if this function returns true, an interaction may have
    ///     occured, as starting a duplicate DoAfter may cancel currently running DoAfters.
    /// </summary>
    /// <param name="args">The DoAfter arguments</param>
    /// <param name="component">The user's DoAfter component</param>
    /// <returns></returns>
    public bool TryStartDoAfter(DoAfterArgs args, DoAfterComponent? component = null)
        => TryStartDoAfter(args, out _, component);

    /// <summary>
    ///     Attempts to start a new DoAfter. Note that even if this function returns false, an interaction may have
    ///     occured, as starting a duplicate DoAfter may cancel currently running DoAfters.
    /// </summary>
    /// <param name="args">The DoAfter arguments</param>
    /// <param name="id">The Id of the newly started DoAfter</param>
    /// <param name="comp">The user's DoAfter component</param>
    /// <returns></returns>
    public bool TryStartDoAfter(DoAfterArgs args, [NotNullWhen(true)] out DoAfterId? id, DoAfterComponent? comp = null)
    {
        DebugTools.Assert(args.Broadcast || Exists(args.EventTarget) || args.Event.GetType() == typeof(AwaitedDoAfterEvent));
        DebugTools.Assert(args.Event.GetType().HasCustomAttribute<NetSerializableAttribute>()
            || args.Event.GetType().Namespace is {} ns && ns.StartsWith("Content.IntegrationTests"), // classes defined in tests cannot be marked as serializable.
            $"Do after event is not serializable. Event: {args.Event.GetType()}");

        if (!Resolve(args.User, ref comp))
        {
            Log.Error($"Attempting to start a doAfter with invalid user: {ToPrettyString(args.User)}.");
            id = null;
            return false;
        }

        // Duplicate blocking & cancellation.
        if (!ProcessDuplicates(args, comp))
        {
            id = null;
            return false;
        }

        id = new DoAfterId(args.User, comp.NextId++);
        var doAfter = new DoAfter(id.Value.Index, args, GameTiming.CurTime);

        // Networking yay
        args.NetTarget = GetNetEntity(args.Target);
        args.NetUsed = GetNetEntity(args.Used);
        args.NetUser = GetNetEntity(args.User);
        args.NetEventTarget = GetNetEntity(args.EventTarget);

        if (args.BreakOnMove)
            doAfter.UserPosition = Transform(args.User).Coordinates;

        if (args.Target != null && args.BreakOnMove)
        {
            var targetPosition = Transform(args.Target.Value).Coordinates;
            doAfter.UserPosition.TryDistance(EntityManager, targetPosition, out doAfter.TargetDistance);
        }

        doAfter.NetUserPosition = GetNetCoordinates(doAfter.UserPosition);

        // For this we need to stay on the same hand slot and need the same item in that hand slot
        // (or if there is no item there we need to keep it free).
        if (args.NeedHand && args.BreakOnHandChange)
        {
            if (!TryComp(args.User, out HandsComponent? handsComponent))
                return false;

            doAfter.InitialHand = handsComponent.ActiveHand?.Name;
            doAfter.InitialItem = handsComponent.ActiveHandEntity;
        }

        doAfter.NetInitialItem = GetNetEntity(doAfter.InitialItem);

        // Initial checks
        if (ShouldCancel(doAfter, GetEntityQuery<TransformComponent>(), GetEntityQuery<HandsComponent>()))
            return false;

        if (args.AttemptFrequency == AttemptFrequency.StartAndEnd && !TryAttemptEvent(doAfter))
            return false;

        if (args.Delay <= TimeSpan.Zero ||
            _tag.HasTag(args.User, "InstantDoAfters"))
        {
            RaiseDoAfterEvents(doAfter, comp);
            // We don't store instant do-afters. This is just a lazy way of hiding them from client-side visuals.
            return true;
        }

        comp.DoAfters.Add(doAfter.Index, doAfter);
        EnsureComp<ActiveDoAfterComponent>(args.User);
        Dirty(args.User, comp);
        args.Event.DoAfter = doAfter;
        return true;
    }

    /// <summary>
    ///     Cancel any applicable duplicate DoAfters and return whether or not the new DoAfter should be created.
    /// </summary>
    private bool ProcessDuplicates(DoAfterArgs args, DoAfterComponent component)
    {
        var blocked = false;
        foreach (var existing in component.DoAfters.Values)
        {
            if (existing.Cancelled || existing.Completed)
                continue;

            if (!IsDuplicate(existing.Args, args))
                continue;

            blocked = blocked | args.BlockDuplicate | existing.Args.BlockDuplicate;

            if (args.CancelDuplicate || existing.Args.CancelDuplicate)
                Cancel(args.User, existing.Index, component);
        }

        return !blocked;
    }

    private bool IsDuplicate(DoAfterArgs args, DoAfterArgs otherArgs)
    {
        if (IsDuplicate(args, otherArgs, args.DuplicateCondition))
            return true;

        if (args.DuplicateCondition == otherArgs.DuplicateCondition)
            return false;

        return IsDuplicate(args, otherArgs, otherArgs.DuplicateCondition);
    }

    private bool IsDuplicate(DoAfterArgs args, DoAfterArgs otherArgs, DuplicateConditions conditions )
    {
        if ((conditions & DuplicateConditions.SameTarget) != 0
            && args.Target != otherArgs.Target)
        {
            return false;
        }

        if ((conditions & DuplicateConditions.SameTool) != 0
            && args.Used != otherArgs.Used)
        {
            return false;
        }

        if ((conditions & DuplicateConditions.SameEvent) != 0
            && args.Event.GetType() != otherArgs.Event.GetType())
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Cancellation
    /// <summary>
    ///     Cancels an active DoAfter.
    /// </summary>
    public void Cancel(DoAfterId? id, DoAfterComponent? comp = null)
    {
        if (id != null)
            Cancel(id.Value.Uid, id.Value.Index, comp);
    }

    /// <summary>
    ///     Cancels an active DoAfter.
    /// </summary>
    public void Cancel(EntityUid entity, ushort id, DoAfterComponent? comp = null)
    {
        if (!Resolve(entity, ref comp, false))
            return;

        if (!comp.DoAfters.TryGetValue(id, out var doAfter))
        {
            Log.Error($"Attempted to cancel do after with an invalid id ({id}) on entity {ToPrettyString(entity)}");
            return;
        }

        InternalCancel(doAfter, comp);
        Dirty(entity, comp);
    }

    private void InternalCancel(DoAfter doAfter, DoAfterComponent component)
    {
        if (doAfter.Cancelled || doAfter.Completed)
            return;

        // Caller is responsible for dirtying the component.
        doAfter.CancelledTime = GameTiming.CurTime;
        RaiseDoAfterEvents(doAfter, component);
    }
    #endregion

    #region Query
    /// <summary>
    ///     Returns the current status of a DoAfter
    /// </summary>
    public DoAfterStatus GetStatus(DoAfterId? id, DoAfterComponent? comp = null)
    {
        if (id != null)
            return GetStatus(id.Value.Uid, id.Value.Index, comp);
        else
            return DoAfterStatus.Invalid;
    }

    /// <summary>
    ///     Returns the current status of a DoAfter
    /// </summary>
    public DoAfterStatus GetStatus(EntityUid entity, ushort id, DoAfterComponent? comp = null)
    {
        if (!Resolve(entity, ref comp, false))
            return DoAfterStatus.Invalid;

        if (!comp.DoAfters.TryGetValue(id, out var doAfter))
            return DoAfterStatus.Invalid;

        if (doAfter.Cancelled)
            return DoAfterStatus.Cancelled;

        if (!doAfter.Completed)
            return DoAfterStatus.Running;

        // Theres the chance here that the DoAfter hasn't actually finished yet if the system's update hasn't run yet.
        // This would also mean the post-DoAfter checks haven't run yet. But whatever, I can't be bothered tracking and
        // networking whether a do-after has raised its events or not.
        return DoAfterStatus.Finished;
    }
    #endregion
}
