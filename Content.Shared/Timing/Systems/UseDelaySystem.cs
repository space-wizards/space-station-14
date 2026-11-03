using Content.Shared.Timing.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Timing.Systems;

public sealed partial class UseDelaySystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    public const string DefaultId = "default";

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnDelayGetState(Entity<UseDelayComponent> ent, ref ComponentGetState args)
    {
        args.State = new UseDelayComponentState()
        {
            Delays = ent.Comp.Delays
        };
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<UseDelayComponent> ent, ref MapInitEvent args)
    {
        // Set default delay length from the prototype
        // This makes it easier for simple use cases that only need a single delay
        SetLength((ent, ent.Comp), ent.Comp.Delay, DefaultId);
    }

    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<UseDelayComponent> ent, ref EntityUnpausedEvent args)
    {
        // We have to do this manually, since it's not just a single field.
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.EndTime += args.PausedTime;
        }
    }
}
