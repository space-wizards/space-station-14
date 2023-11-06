using Content.Server.Explosion.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.IgnitionSource;

/// <summary>
/// Handles igniting when triggered and stopping ignition after the delay.
/// </summary>
public sealed class IgniteOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IgnitionSourceSystem _source = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var query = EntityQueryEnumerator<IgniteOnTriggerComponent, IgnitionSourceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            if (!source.Ignited)
                continue;

            if (_timing.CurTime < comp.IgnitedUntil)
                continue;

            _source.SetIgnited((uid, source), false);
        }
    }

    private void OnTrigger(Entity<IgniteOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // prevent spamming sound and ignition
        TryComp<UseDelayComponent>(ent, out var delay);
        if (_useDelay.ActiveDelay(ent, delay))
            return;

        _source.SetIgnited(ent.Owner);
        _audio.PlayPvs(ent.Comp.IgniteSound, ent);

        _useDelay.BeginDelay(ent, delay);
        ent.Comp.IgnitedUntil = _timing.CurTime + ent.Comp.IgnitedTime;
    }
}
