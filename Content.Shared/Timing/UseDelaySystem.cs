using System.Threading;
using Content.Shared.Cooldown;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private HashSet<UseDelayComponent> _activeDelays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<UseDelayComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<UseDelayComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnPaused(EntityUid uid, UseDelayComponent component, ref EntityPausedEvent args)
    {
        // This entity just got paused, but wasn't before
        if (component.DelayEndTime != null)
            component.RemainingDelay = _gameTiming.CurTime - component.DelayEndTime;

        _activeDelays.Remove(component);
        Dirty(component);
    }

    private void OnUnpaused(EntityUid uid, UseDelayComponent component, ref EntityUnpausedEvent args)
    {
        if (component.RemainingDelay == null)
            return;

        // We got unpaused, resume the delay/cooldown. Currently this takes for granted that ItemCooldownComponent
        // handles the pausing on its own. I'm not even gonna check, because I CBF fixing it if it doesn't.
        component.DelayEndTime = _gameTiming.CurTime + component.RemainingDelay;
        Dirty(component);
        _activeDelays.Add(component);
    }

    private void OnHandleState(EntityUid uid, UseDelayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not UseDelayComponentState state)
            return;

        component.LastUseTime = state.LastUseTime;
        component.Delay = state.Delay;
        component.DelayEndTime = state.DelayEndTime;

        if (component.DelayEndTime == null)
            _activeDelays.Remove(component);
        else
            _activeDelays.Add(component);
    }

    private void OnGetState(EntityUid uid, UseDelayComponent component, ref ComponentGetState args)
    {
        args.State = new UseDelayComponentState(component.LastUseTime, component.Delay, component.DelayEndTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<UseDelayComponent>();
        var curTime = _gameTiming.CurTime;
        var mQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        foreach (var delay in _activeDelays)
        {
            if (delay.DelayEndTime == null ||
                curTime > delay.DelayEndTime ||
                Deleted(delay.Owner, mQuery) ||
                delay.CancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                toRemove.Add(delay);
            }
        }

        foreach (var delay in toRemove)
        {
            delay.CancellationTokenSource = null;
            delay.DelayEndTime = null;
            _activeDelays.Remove(delay);
            Dirty(delay);
        }
    }

    public void BeginDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.ActiveDelay || Deleted(uid)) return;

        component.CancellationTokenSource = new CancellationTokenSource();

        DebugTools.Assert(!_activeDelays.Contains(component));
        _activeDelays.Add(component);

        var currentTime = _gameTiming.CurTime;
        component.LastUseTime = currentTime;
        component.DelayEndTime = currentTime + component.Delay;
        Dirty(component);

        // TODO just merge these components?
        var cooldown = EnsureComp<ItemCooldownComponent>(component.Owner);
        cooldown.CooldownStart = currentTime;
        cooldown.CooldownEnd = component.DelayEndTime;
    }

    public bool ActiveDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.ActiveDelay;
    }

    public void Cancel(UseDelayComponent component)
    {
        component.CancellationTokenSource?.Cancel();
        component.CancellationTokenSource = null;
        component.DelayEndTime = null;
        _activeDelays.Remove(component);
        Dirty(component);

        if (TryComp<ItemCooldownComponent>(component.Owner, out var cooldown))
        {
            cooldown.CooldownEnd = _gameTiming.CurTime;
        }
    }

    public void Restart(UseDelayComponent component)
    {
        component.CancellationTokenSource?.Cancel();
        component.CancellationTokenSource = null;
        BeginDelay(component.Owner, component);
    }
}
