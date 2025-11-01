using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public const string DefaultId = "default";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<UseDelayComponent, ComponentGetState>(OnDelayGetState);
        SubscribeLocalEvent<UseDelayComponent, ComponentHandleState>(OnDelayHandleState);
        SubscribeLocalEvent<UseDelayComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<UseDelayComponent> ent, ref ComponentInit args)
    {
        ResetAllDelays(ent);

    }

    private void OnDelayHandleState(Entity<UseDelayComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not UseDelayComponentState state)
            return;

        ent.Comp.Delays.Clear();

        // At time of writing sourcegen networking doesn't deep copy so this will mispredict if you try.
        foreach (var (key, delay) in state.Delays)
        {
            ent.Comp.Delays[key] = new UseDelayInfo(delay.Length, delay.StartTime, delay.EndTime);
        }
    }

    private void OnDelayGetState(Entity<UseDelayComponent> ent, ref ComponentGetState args)
    {
        args.State = new UseDelayComponentState()
        {
            Delays = ent.Comp.Delays
        };
    }

    private void OnMapInit(Entity<UseDelayComponent> ent, ref MapInitEvent args)
    {
        // Set default delay length from the prototype
        // This makes it easier for simple use cases that only need a single delay
        SetLength((ent, ent.Comp), ent.Comp.Delay, DefaultId);
    }

    private void OnUnpaused(Entity<UseDelayComponent> ent, ref EntityUnpausedEvent args)
    {
        // We have to do this manually, since it's not just a single field.
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.EndTime += args.PausedTime;
        }
    }

    /// <summary>
    /// Sets the length of the delay with the specified ID.
    /// </summary>
    /// <remarks>
    /// This will add a UseDelay component to the entity if it doesn't have one.
    /// </remarks>
    public bool SetLength(Entity<UseDelayComponent?> ent, TimeSpan length, string id = DefaultId)
    {
        EnsureComp<UseDelayComponent>(ent.Owner, out var comp);

        if (comp.Delays.TryGetValue(id, out var entry))
        {
            if (entry.Length == length)
                return true;

            entry.Length = length;
        }
        else
        {
            comp.Delays.Add(id, new UseDelayInfo(length));
        }

        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Returns true if the entity has a currently active UseDelay with the specified ID.
    /// </summary>
    public bool IsDelayed(Entity<UseDelayComponent?> ent, string id = DefaultId)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        return entry.EndTime >= _gameTiming.CurTime;
    }

    /// <summary>
    /// Cancels the delay with the specified ID.
    /// </summary>
    public void CancelDelay(Entity<UseDelayComponent> ent, string id = DefaultId)
    {
        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return;

        entry.EndTime = _gameTiming.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Tries to get info about the delay with the specified ID. See <see cref="UseDelayInfo"/>.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="info"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool TryGetDelayInfo(Entity<UseDelayComponent?> ent, [NotNullWhen(true)] out UseDelayInfo? info, string id = DefaultId)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
        {
            info = null;
            return false;
        }

        return ent.Comp.Delays.TryGetValue(id, out info);
    }

    /// <summary>
    /// Returns info for the delay that will end farthest in the future.
    /// </summary>
    public UseDelayInfo GetLastEndingDelay(Entity<UseDelayComponent> ent)
    {
        if (!ent.Comp.Delays.TryGetValue(DefaultId, out var last))
            return new UseDelayInfo(TimeSpan.Zero);

        foreach (var entry in ent.Comp.Delays)
        {
            if (entry.Value.EndTime > last.EndTime)
                last = entry.Value;
        }
        return last;
    }

    /// <summary>
    /// Resets the delay with the specified ID for this entity if possible.
    /// </summary>
    /// <param name="checkDelayed">Check if the entity has an ongoing delay with the specified ID.
    /// If it does, return false and don't reset it.
    /// Otherwise reset it and return true.</param>
    public bool TryResetDelay(Entity<UseDelayComponent> ent, bool checkDelayed = false, string id = DefaultId)
    {
        if (checkDelayed && IsDelayed((ent.Owner, ent.Comp), id))
            return false;

        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        var curTime = _gameTiming.CurTime;
        entry.StartTime = curTime;
        entry.EndTime = curTime - _metadata.GetPauseTime(ent) + entry.Length;
        Dirty(ent);
        return true;
    }

    public bool TryResetDelay(EntityUid uid, bool checkDelayed = false, UseDelayComponent? component = null, string id = DefaultId)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return TryResetDelay((uid, component), checkDelayed, id);
    }

    /// <summary>
    /// Resets all delays on the entity.
    /// </summary>
    public void ResetAllDelays(Entity<UseDelayComponent> ent)
    {
        var curTime = _gameTiming.CurTime;
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.StartTime = curTime;
            entry.EndTime = curTime - _metadata.GetPauseTime(ent) + entry.Length;
        }
        Dirty(ent);
    }
}
