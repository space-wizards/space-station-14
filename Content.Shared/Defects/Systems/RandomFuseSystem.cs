using Content.Shared.Defects.Components;
using Content.Shared.Trigger.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Defects.Systems;

// Randomizes TimerTriggerComponent.Delay at spawn for entities with
// RandomFuseComponent, so each instance has a unique fuse countdown.
public sealed class RandomFuseSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomFuseComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomFuseComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<TimerTriggerComponent>(ent.Owner, out var timer))
            return;

        timer.Delay = TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.MinDelay, ent.Comp.MaxDelay));
    }
}
